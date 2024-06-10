from __future__ import annotations

import gzip
from pathlib import Path
from typing import cast

import bpy
import numpy as np
from bpy.types import Action, ArmatureModifier, ByteColorAttribute, EditBone, FCurve, Object, PoseBone
from mathutils import Matrix, Quaternion, Vector
from math import *

from ..importer.classes import (
    ANIM_IDENTIFIER,
    MAGIC,
    MODEL_IDENTIFIER,
    WORLD_IDENTIFIER,
    Bone,
    ConvexCollision,
    EUEFormatVersion,
    Material,
    MorphTarget,
    Socket,
    UEAnim,
    UEModel,
    UEModelLOD,
    UEModelSkeleton,
    VertexColor,
    Weight,
    UEWorld,
)
from ..importer.reader import FArchiveReader
from ..importer.utils import get_active_armature, get_case_insensitive, make_axis_vector, make_quat, make_vector
from ..logging import Log
from ..options import UEAnimOptions, UEFormatOptions, UEModelOptions, UEWorldOptions


class UEFormatImport:
    def __init__(self, options: UEFormatOptions) -> None:
        self.options = options

    def import_file(self, path: str | Path) -> Object | Action:
        path = path if isinstance(path, Path) else Path(path)

        Log.time_start(f"Import {path}")

        with path.open("rb") as file:
            obj = self.import_data(file.read())

        Log.time_end(f"Import {path}")

        return obj

    def import_data(self, data: bytes) -> Object | Action:
        with FArchiveReader(data) as ar:
            return self.import_data_by_reader(ar)
    def import_data_by_reader(self, ar: FArchiveReader) -> Object | Action:
        magic = ar.read_string(len(MAGIC))
        if magic != MAGIC:
            msg = "Invalid magic"
            raise ValueError(msg)

        identifier = ar.read_fstring()
        self.file_version = EUEFormatVersion(int.from_bytes(ar.read_byte(), byteorder="big"))
        if self.file_version > EUEFormatVersion.LatestVersion:
            msg = f"File Version {self.file_version} is not supported for this version of the importer."
            Log.error(msg)
            raise ValueError(msg)
        object_name = ar.read_fstring()
        Log.info(f"Importing {object_name}")

        read_archive = ar
        is_compressed = ar.read_bool()
        if is_compressed:
            from .. import zstd_decompressor

            compression_type = ar.read_fstring()
            uncompressed_size = ar.read_int()
            _compressed_size = ar.read_int()

            if compression_type == "GZIP":
                read_archive = FArchiveReader(gzip.decompress(ar.read_to_end()))
            elif compression_type == "ZSTD":
                read_archive = FArchiveReader(
                    zstd_decompressor.decompress(
                        ar.read_to_end(),
                        uncompressed_size,
                    ),
                )
            else:
                msg = f"Unknown Compression Type: {compression_type}"
                Log.error(msg)
                raise ValueError(msg)

        if identifier == MODEL_IDENTIFIER:
            return self.import_uemodel_data(read_archive, object_name)
        if identifier == ANIM_IDENTIFIER:
            return self.import_ueanim_data(read_archive, object_name)
        if identifier == WORLD_IDENTIFIER:
            return self.import_ueworld_data(read_archive, object_name)

        msg = f"Unknown identifier: {identifier}"
        Log.error(msg)
        raise ValueError

    # TODO: clean up code quality, esp in the skeleton department  # noqa: TD002, FIX002, TD003
    def import_uemodel_data(self, ar: FArchiveReader, name: str) -> Object:
        assert isinstance(self.options, UEModelOptions)  # noqa: S101

        data: UEModel
        if self.file_version >= EUEFormatVersion.LevelOfDetailFormatRestructure:
            data = UEModel.from_archive(ar, self.options.scale_factor)
        else:
            data = self.deserialize_model_legacy(ar)

        # meshes
        created_lods: list[Object] = []
        for lod in data.lods:
            lod_name = f"{name}_{lod.name}"
            mesh_data = bpy.data.meshes.new(lod_name)

            mesh_data.from_pydata(lod.vertices, [], lod.indices)  # type: ignore[reportArgumentType]

            mesh_object = bpy.data.objects.new(lod_name, mesh_data)
            return_object = mesh_object
            if self.options.link:
                bpy.context.collection.objects.link(mesh_object)

            # normals
            if len(lod.normals) > 0:
                mesh_data.polygons.foreach_set("use_smooth", [True] * len(mesh_data.polygons))
                mesh_data.normals_split_custom_set_from_vertices(lod.normals)  # type: ignore[reportArgumentType]

                if bpy.app.version < (4, 1, 0):  # type: ignore[reportOperatorIssue]
                    mesh_data.use_auto_smooth = True

            # weights
            if lod.weights and data.skeleton and data.skeleton.bones:
                for weight in lod.weights:
                    bone_name = data.skeleton.bones[weight.bone_index].name
                    vertex_group = mesh_object.vertex_groups.get(bone_name)
                    if not vertex_group:
                        vertex_group = mesh_object.vertex_groups.new(name=bone_name)
                    vertex_group.add([weight.vertex_index], weight.weight, "ADD")

            # morph targets
            if self.options.import_morph_targets and lod.morphs:
                default_key = mesh_object.shape_key_add(from_mix=False)
                default_key.name = "Default"
                default_key.interpolation = "KEY_LINEAR"

                for morph in lod.morphs:
                    key = mesh_object.shape_key_add(from_mix=False)
                    key.name = morph.name
                    key.interpolation = "KEY_LINEAR"

                    for delta in morph.deltas:
                        key.data[delta.vertex_index].co += Vector(delta.position)

            squish = lambda array: array.reshape(
                array.size,
            )  # Squish nD array into 1D array (required by foreach_set).
            do_remapping = lambda array, indices: array[indices]

            vertices = [vertex for polygon in mesh_data.polygons for vertex in polygon.vertices]
            # indices = np.array([index for polygon in mesh_data.polygons for index in polygon.loop_indices], dtype=np.int32)
            # assert np.all(indices[:-1] <= indices[1:]) # check if indices are sorted hmm idk
            for color_info in lod.colors:
                remapped = do_remapping(color_info.data, vertices)
                vertex_color = cast(
                    ByteColorAttribute,
                    mesh_data.color_attributes.new(
                        domain="CORNER",
                        type="BYTE_COLOR",
                        name=color_info.name,
                    ),
                )
                vertex_color.data.foreach_set("color", squish(remapped))

            for i, uvs in enumerate(lod.uvs):
                remapped = do_remapping(uvs, vertices)
                uv_layer = mesh_data.uv_layers.new(name="UV" + str(i))
                uv_layer.data.foreach_set("uv", squish(remapped))

            # materials
            if lod.materials:
                for i, material in enumerate(lod.materials):
                    mat = bpy.data.materials.get(material.material_name)
                    if mat is None:
                        mat = bpy.data.materials.new(name=material.material_name)
                    mesh_data.materials.append(mat)

                    start_face_index = material.first_index // 3
                    end_face_index = start_face_index + material.num_faces
                    for face_index in range(start_face_index, end_face_index):
                        mesh_data.polygons[face_index].material_index = i

            created_lods.append(mesh_object)

            if not self.options.import_lods:
                break

        # skeleton
        if data.skeleton and (data.skeleton.bones or (self.options.import_sockets and data.skeleton.sockets)):
            armature_data = bpy.data.armatures.new(name=name)
            armature_data.display_type = "STICK"

            template_armature_object = bpy.data.objects.new(name + "_Template_Skeleton", armature_data)
            template_armature_object.show_in_front = True
            bpy.context.collection.objects.link(template_armature_object)
            bpy.context.view_layer.objects.active = template_armature_object
            template_armature_object.select_set(state=True)

            if data.skeleton.bones:
                # create bones
                bpy.ops.object.mode_set(mode="EDIT")
                edit_bones = armature_data.edit_bones
                for bone_ in data.skeleton.bones:
                    bone_pos = Vector(bone_.position)
                    bone_rot = Quaternion(  # xyzw -> wxyz
                        (
                            bone_.rotation[3],
                            bone_.rotation[0],
                            bone_.rotation[1],
                            bone_.rotation[2],
                        ),
                    )

                    edit_bone = edit_bones.new(bone_.name)
                    edit_bone["orig_loc"] = bone_pos
                    # TODO: unravel all these conjugations wtf, it works so imma leave it but jfc it's awful  # noqa: TD003, TD002, FIX002
                    edit_bone["orig_quat"] = bone_rot.conjugated()
                    edit_bone.length = self.options.bone_length * self.options.scale_factor

                    bone_matrix = Matrix.Translation(bone_pos) @ bone_rot.to_matrix().to_4x4()

                    if bone_.parent_index >= 0:
                        parent_bone = cast(
                            EditBone | None,
                            edit_bones.get(
                                data.skeleton.bones[bone_.parent_index].name,
                            ),
                        )
                        assert parent_bone  # noqa: S101
                        edit_bone.parent = parent_bone
                        bone_matrix = cast(Matrix, parent_bone.matrix) @ bone_matrix

                    edit_bone.matrix = bone_matrix  # type: ignore[reportAttributeAccessIssue]

                    if not self.options.reorient_bones:
                        edit_bone["post_quat"] = bone_rot

                bpy.ops.object.mode_set(mode="OBJECT")

            # sockets
            if self.options.import_sockets and data.skeleton.sockets:
                # create sockets
                bpy.ops.object.mode_set(mode="EDIT")
                socket_collection = armature_data.collections.new("Sockets")
                for socket in data.skeleton.sockets:
                    socket_bone = edit_bones.new(socket.name)
                    socket_collection.assign(socket_bone)
                    socket_bone["is_socket"] = True
                    parent_bone = cast(
                        EditBone | None,
                        get_case_insensitive(
                            edit_bones,
                            socket.parent_name,
                        ),
                    )
                    if parent_bone is None:
                        continue
                    socket_bone.parent = parent_bone
                    socket_bone.length = self.options.bone_length * self.options.scale_factor
                    socket_bone.matrix = (
                        cast(Matrix, parent_bone.matrix)
                        @ Matrix.Translation(socket.position)
                        @ Quaternion(  # xyzw -> wxyz
                            (
                                socket.rotation[3],
                                socket.rotation[0],
                                socket.rotation[1],
                                socket.rotation[2],
                            ),
                        )
                        .to_matrix()
                        .to_4x4()
                    )  # type: ignore[reportAttributeAccessIssue]

                bpy.ops.object.mode_set(mode="OBJECT")

            if data.skeleton.bones and self.options.reorient_bones:
                bpy.ops.object.mode_set(mode="EDIT")

                for bone in armature_data.edit_bones:
                    bone: EditBone
                    if bone.get("is_socket"):
                        continue

                    children = []
                    for child in bone.children:  # type: ignore[reportOptionalIterable]
                        if child.get("is_socket"):
                            continue

                        children.append(child)

                    if len(children) == 0 and bone.parent is None:
                        continue

                    target_length = bone.length
                    if len(children) == 0:
                        new_rot = Vector(bone.parent["reorient_direction"])
                        new_rot.rotate(Quaternion(bone["orig_quat"]).conjugated())

                        target_rotation = make_axis_vector(new_rot)
                    else:
                        avg_child_pos = Vector()
                        avg_child_length = 0.0
                        for child in children:
                            pos = Vector(child["orig_loc"])
                            avg_child_pos += pos
                            avg_child_length += pos.length

                        avg_child_pos /= len(children)
                        avg_child_length /= len(children)

                        target_rotation = make_axis_vector(avg_child_pos)
                        bone["reorient_direction"] = target_rotation

                        target_length = avg_child_length

                    post_quat = Vector((0, 1, 0)).rotation_difference(target_rotation)
                    bone.matrix @= post_quat.to_matrix().to_4x4()  # type: ignore[reportOperatorIssue]
                    bone.length = max(0.01, target_length)

                    post_quat.rotate(Quaternion(bone["orig_quat"]).conjugated())
                    bone["post_quat"] = post_quat

                bpy.ops.object.mode_set(mode="OBJECT")

            if created_lods:
                bpy.data.objects.remove(template_armature_object)
            else:
                template_armature_object.name = name
                return_object = template_armature_object

                # this is the same functionality as in unreal dont @ me
                if self.options.import_virtual_bones:
                    bpy.ops.object.mode_set(mode="EDIT")
                    virtual_bone_collection = armature_data.collections.new("Virtual Bones")
                    edit_bones = armature_data.edit_bones
                    for virtual in data.skeleton.virtual_bones:
                        source_bone = edit_bones.get(virtual.source_name)
                        if source_bone is None:
                            continue

                        virtual_bone = edit_bones.new(virtual.virtual_name)
                        virtual_bone_collection.assign(virtual_bone)
                        virtual_bone.head = source_bone.tail
                        virtual_bone.tail = source_bone.head

                    bpy.ops.object.mode_set(mode="POSE")

                    for virtual in data.skeleton.virtual_bones:
                        virtual_bone = template_armature_object.pose.bones.get(
                            virtual.virtual_name,
                        )
                        if virtual_bone is None:
                            continue

                        constraint = virtual_bone.constraints.new("IK")
                        constraint.target = template_armature_object
                        constraint.subtarget = virtual.target_name
                        constraint.chain_count = 1
                        virtual_bone.ik_stretch = 1

                    bpy.ops.object.mode_set(mode="OBJECT")

                # bone colors
                for bone in template_armature_object.pose.bones:
                    if bone.children and len(bone.children) == 0:
                        bone.color.palette = "THEME03"

                # socket colors
                for socket in data.skeleton.sockets:
                    socket_bone = template_armature_object.pose.bones.get(socket.name)
                    if socket_bone is not None:
                        socket_bone.color.palette = "THEME05"

                # virtual bone colors
                for virtual in data.skeleton.virtual_bones:
                    virtual_bone = template_armature_object.pose.bones.get(
                        virtual.virtual_name,
                    )
                    if virtual_bone is not None:
                        virtual_bone.color.palette = "THEME11"

            for lod in created_lods:
                armature_object = bpy.data.objects.new(
                    lod.name + "_Skeleton",
                    armature_data,
                )
                armature_object.show_in_front = True
                return_object = armature_object

                if self.options.link:
                    bpy.context.collection.objects.link(armature_object)
                bpy.context.view_layer.objects.active = armature_object
                armature_object.select_set(state=True)

                lod.parent = armature_object

                # armature modifier
                armature_modifier = cast(
                    ArmatureModifier,
                    lod.modifiers.new(
                        armature_object.name,
                        type="ARMATURE",
                    ),
                )
                armature_modifier.show_expanded = False
                armature_modifier.use_vertex_groups = True
                armature_modifier.object = armature_object

                bpy.ops.object.mode_set(mode="POSE")

                # bone colors
                for bone in armature_object.pose.bones:
                    if lod.vertex_groups.get(bone.name) is None:
                        bone.color.palette = "THEME14"
                        continue

                    if bone.children and len(bone.children) == 0:
                        bone.color.palette = "THEME03"

                # socket colors
                for socket in data.skeleton.sockets:
                    socket_bone = armature_object.pose.bones.get(socket.name)
                    if socket_bone is not None:
                        socket_bone.color.palette = "THEME05"

                # virtual bone colors
                for virtual in data.skeleton.virtual_bones:
                    virtual_bone = armature_object.pose.bones.get(virtual.virtual_name)
                    if virtual_bone is not None:
                        virtual_bone.color.palette = "THEME11"

                bpy.ops.object.mode_set(mode="OBJECT")

        # collision
        if self.options.import_collision and data.collisions:
            for index, collision in enumerate(data.collisions):
                collision_name = index if collision.name == "None" else collision.name
                collision_object_name = name + f"_Collision_{collision_name}"
                collision_mesh_data = bpy.data.meshes.new(collision_object_name)
                collision_mesh_data.from_pydata(collision.vertices, [], collision.indices)  # type: ignore[reportArgumentType]

                collision_mesh_object = bpy.data.objects.new(collision_object_name, collision_mesh_data)
                collision_mesh_object.display_type = "WIRE"
                if self.options.link:
                    bpy.context.collection.objects.link(collision_mesh_object)

        return return_object

    def import_ueanim_data(self, ar: FArchiveReader, name: str) -> Action:
        assert isinstance(self.options, UEAnimOptions)  # noqa: S101

        data = UEAnim.from_archive(ar, self.options.scale_factor)

        action = bpy.data.actions.new(name=name)

        armature = self.options.override_skeleton or get_active_armature()
        assert isinstance(armature, bpy.types.Object)  # noqa: S101

        if self.options.link:
            armature.animation_data_create()
            armature.animation_data.action = action

        # bone anim data
        pose_bones = armature.pose.bones
        for track in data.tracks:
            bone = get_case_insensitive(pose_bones, track.name)

            if bone is None:
                continue

            def create_fcurves(
                name: str,
                count: int,
                key_count: int | None,
                bone: PoseBone,
            ) -> list[FCurve]:
                path = bone.path_from_id(name)
                curves: list[FCurve] = []
                for i in range(count):
                    curve = action.fcurves.new(path, index=i)
                    curve.keyframe_points.add(key_count)
                    curves.append(curve)
                return curves

            def add_key(
                curves: list[FCurve],
                vector: Vector | list[float] | Quaternion,
                key_index: int,
                frame: int,
            ) -> None:
                for i in range(len(vector)):
                    curves[i].keyframe_points[key_index].co = frame, vector[i]
                    curves[i].keyframe_points[key_index].interpolation = "LINEAR"
                    curves[i].keyframe_points[key_index].interpolation = "LINEAR"

            orig_loc = Vector(orig_loc) if (orig_loc := bone.bone.get("orig_loc")) else Vector()
            orig_quat = Quaternion(orig_quat) if (orig_quat := bone.bone.get("orig_quat")) else Quaternion()
            post_quat = Quaternion(post_quat) if (post_quat := bone.bone.get("post_quat")) else Quaternion()

            if not self.options.rotation_only:
                loc_curves = create_fcurves("location", 3, len(track.position_keys), bone)
                scale_curves = create_fcurves("scale", 3, len(track.scale_keys), bone)
                for index, key in enumerate(track.position_keys):
                    pos = key.get_vector()
                    pos -= orig_loc
                    pos.rotate(post_quat.conjugated())

                    add_key(loc_curves, pos, index, key.frame)

                for index, key in enumerate(track.scale_keys):
                    add_key(scale_curves, key.value, index, key.frame)

            rot_curves = create_fcurves("rotation_quaternion", 4, len(track.rotation_keys), bone)
            for index, key in enumerate(track.rotation_keys):
                p_quat = key.get_quat().conjugated()

                q = post_quat.copy()
                q.rotate(orig_quat)

                quat = q

                q = post_quat.copy()

                q.rotate(p_quat)
                quat.rotate(q.conjugated())

                add_key(rot_curves, quat, index, key.frame)

            bone.matrix_basis.identity()  # type: ignore[reportAttributeAccessIssue]

        return action

    def import_ueworld_data(self, ar: FArchiveReader, name: str) -> Object:
        assert isinstance(self.options, UEWorldOptions)  # noqa: S101

        data = UEWorld.from_archive(ar, self.options.scale_factor)
        
        world_parent = bpy.data.objects.new(name, None)
        bpy.context.collection.objects.link(world_parent)
        
        meshes = {}
        mesh_import_settings = UEModelOptions.from_settings(bpy.context.scene.uf_settings)
        mesh_import_settings.link = False
        mesh_importer = UEFormatImport(mesh_import_settings)

        mesh_index = 0
        mesh_total = len(data.meshes)
        for mesh in data.meshes:
            mesh_index += 1
            print(f"{mesh_index} / {mesh_total}")
            meshes[mesh.hash] = mesh_importer.import_data_by_reader(mesh.model_reader)
        
        actor_index = 0
        actor_total = len(data.actors)
        for actor in data.actors:
            actor_index += 1
            print(f"{actor_index} / {actor_total}")
            mesh = meshes[actor.model_hash]
            mesh_data = mesh.data if self.options.instance_meshes else mesh.data.copy()
            
            obj = bpy.data.objects.new(actor.name, mesh_data)
            obj.location = make_vector(actor.location)
            obj.rotation_mode = "QUATERNION"
            obj.rotation_quaternion = make_quat(actor.rotation)
            obj.scale = make_vector(actor.scale)
            obj.parent = world_parent
            bpy.context.scene.collection.objects.link(obj)
        
        return world_parent

    def deserialize_model_legacy(self, ar: FArchiveReader) -> UEModel:
        data = UEModel()
        data.skeleton = UEModelSkeleton()
        lod = UEModelLOD(name="LOD0")

        while not ar.eof():
            header_name = ar.read_fstring()
            array_size = ar.read_int()
            byte_size = ar.read_int()

            pos = ar.data.tell()
            if header_name == "VERTICES":
                flattened = ar.read_float_vector(array_size * 3)
                lod.vertices = (np.array(flattened) * self.options.scale_factor).reshape(array_size, 3)
            elif header_name == "INDICES":
                lod.indices = np.array(ar.read_int_vector(array_size), dtype=np.int32).reshape(array_size // 3, 3)
            elif header_name == "NORMALS":
                if self.file_version >= EUEFormatVersion.SerializeBinormalSign:
                    flattened = np.array(
                        ar.read_float_vector(array_size * 4),
                    )  # W XYZ # TODO: change to XYZ W  # noqa: TD002, FIX002, TD003
                    lod.normals = flattened.reshape(-1, 4)[:, 1:]
                else:
                    flattened = np.array(ar.read_float_vector(array_size * 3)).reshape(array_size, 3)
                    lod.normals = flattened
            elif header_name == "TANGENTS":
                ar.skip(array_size * 3 * 3)
                # flattened = np.array(ar.read_float_vector(array_size * 3)).reshape(array_size, 3)  # noqa: ERA001
            elif header_name == "VERTEXCOLORS":
                if self.file_version >= EUEFormatVersion.AddMultipleVertexColors:
                    lod.colors = [VertexColor.from_archive(ar) for _ in range(array_size)]
                else:
                    lod.colors = [
                        VertexColor(
                            "COL0",
                            (np.array(ar.read_byte_vector(array_size * 4)).reshape(array_size, 4) / 255).astype(
                                np.float32,
                            ),
                        ),
                    ]
            elif header_name == "TEXCOORDS":
                lod.uvs = []
                for _ in range(array_size):
                    count = ar.read_int()
                    lod.uvs.append(np.array(ar.read_float_vector(count * 2)).reshape(count, 2))
            elif header_name == "MATERIALS":
                lod.materials = ar.read_array(array_size, lambda ar: Material.from_archive(ar))
            elif header_name == "WEIGHTS":
                lod.weights = ar.read_array(array_size, lambda ar: Weight.from_archive(ar))
            elif header_name == "MORPHTARGETS":
                lod.morphs = ar.read_array(
                    array_size,
                    lambda ar: MorphTarget.from_archive(ar, self.options.scale_factor),
                )
            elif header_name == "BONES":
                data.skeleton.bones = ar.read_array(
                    array_size,
                    lambda ar: Bone.from_archive(ar, self.options.scale_factor),
                )
            elif header_name == "SOCKETS":
                data.skeleton.sockets = ar.read_array(
                    array_size,
                    lambda ar: Socket.from_archive(ar, self.options.scale_factor),
                )
            elif header_name == "COLLISION" and self.file_version >= EUEFormatVersion.AddConvexCollisionGeom:
                data.collisions = ar.read_array(
                    array_size,
                    lambda ar: ConvexCollision.from_archive(ar, self.options.scale_factor),
                )
            else:
                Log.warn(f"Unknown Data: {header_name}")
                ar.skip(byte_size)
            ar.data.seek(pos + byte_size, 0)

        data.lods.append(lod)

        return data
