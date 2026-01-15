from __future__ import annotations

import gzip
from pathlib import Path
from typing import cast

import bpy
import numpy as np
from bpy.types import Action, ArmatureModifier, ByteColorAttribute, EditBone, FCurve, Object, PoseBone
from bpy_extras import anim_utils
from mathutils import Matrix, Quaternion, Vector
from math import *

from .reorient_utils import reorient_bones

from ..importer.classes import (
    MAGIC,
    MODEL_IDENTIFIER,
    POSE_IDENTIFIER,
    ANIM_IDENTIFIER,
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
    UEPose
)
from ..importer.reader import FArchiveReader
from ..importer.utils import *
from ..logging import Log
from ..options import UEAnimOptions, UEFormatOptions, UEModelOptions, UEPoseOptions


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
        file_version = EUEFormatVersion(int.from_bytes(ar.read_byte(), byteorder="big"))
        if file_version > EUEFormatVersion.LatestVersion:
            msg = f"File Version {file_version} is not supported for this version of the importer."
            Log.error(msg)
            raise ValueError(msg)
        object_name = ar.read_fstring()
        Log.info(f"Importing {object_name}")

        read_archive = ar
        is_compressed = ar.read_bool()
        if is_compressed:

            compression_type = ar.read_fstring()
            uncompressed_size = ar.read_int()
            _compressed_size = ar.read_int()

            if compression_type == "GZIP":
                read_archive = FArchiveReader(gzip.decompress(ar.read_to_end()))
            elif compression_type == "ZSTD":
                from .. import zstd_decompressor
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

        read_archive.file_version = file_version
        read_archive.metadata["scale"] = self.options.scale_factor

        if identifier == MODEL_IDENTIFIER:
            return self.import_uemodel_data(read_archive, object_name)
        if identifier == ANIM_IDENTIFIER:
            return self.import_ueanim_data(read_archive, object_name)
        if identifier == POSE_IDENTIFIER:
            return self.import_uepose_data(read_archive, object_name)

        msg = f"Unknown identifier: {identifier}"
        Log.error(msg)
        raise ValueError

    # TODO: clean up code quality, esp in the skeleton department
    def import_uemodel_data(self, ar: FArchiveReader, name: str) -> tuple[bpy.types.Object, UEModel]:
        assert isinstance(self.options, UEModelOptions)  # noqa: S101

        data: UEModel
        if ar.file_version >= EUEFormatVersion.LevelOfDetailFormatRestructure:
            data = UEModel.from_archive(ar)
        else:
            data = UEModel.from_archive_legacy(ar)

        # meshes
        return_object = None
        target_lod = min(self.options.target_lod, len(data.lods) - 1)
        created_lods: list[Object] = []
        for index, lod in enumerate(data.lods):
            if index != target_lod:
                continue
                
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
                if not mesh_object.data.shape_keys:
                    mesh_object.shape_key_add(name="Basis", from_mix=False)

                for morph in lod.morphs:
                    key = mesh_object.shape_key_add(from_mix=False)
                    key.name = morph.name
                    key.interpolation = "KEY_LINEAR"

                    for delta in morph.deltas:
                        key.data[delta.vertex_index].co += Vector(delta.position)
                        
                    key.value = 0

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

                reorient_bones(
                    armature_data,
                    bone_length=self.options.bone_length * self.options.scale_factor,
                    allowed_reorient_children=self.options.allowed_reorient_children
                )
            
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
                    if not bone.children or len(bone.children) == 0:
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
                    if not (vertex_group := lod.vertex_groups.get(bone.name)) or not has_vertex_weights(lod, vertex_group):
                        bone.color.palette = "THEME14"
                        continue

                    if not bone.children or len(bone.children) == 0:
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
                collision_object_name = f"UCX_{name}_{collision_name}"
                collision_mesh_data = bpy.data.meshes.new(collision_object_name)
                collision_mesh_data.from_pydata(collision.vertices, [], collision.indices)  # type: ignore[reportArgumentType]

                collision_mesh_object = bpy.data.objects.new(collision_object_name, collision_mesh_data)
                collision_mesh_object.display_type = "WIRE"
                if self.options.link:
                    bpy.context.collection.objects.link(collision_mesh_object)

        return return_object, data

    def import_ueanim_data(self, ar: FArchiveReader, name: str) -> tuple[bpy.types.Action, UEAnim]:
        assert isinstance(self.options, UEAnimOptions)  # noqa: S101

        data = UEAnim.from_archive(ar)

        action = bpy.data.actions.new(name=name)

        armature = self.options.override_skeleton or get_active_armature()
        assert isinstance(armature, bpy.types.Object)  # noqa: S101
        
        if armature_anim_data := armature.animation_data:
            armature_anim_data.action = None
        
        if self.options.link:
            armature.animation_data_create()
            armature.animation_data.action = action

        if self.options.link and bpy.app.version >= (4, 4, 0):
            slot = action.slots.new(id_type='OBJECT', name=f"Slot_{armature.name}")
            armature.animation_data.action_slot = slot
            

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
                    if bpy.app.version < (5, 0, 0):
                        curve = action.fcurves.new(path, index=i)
                    else:
                        slot = action.slots[0] if len(action.slots) > 0 else action.slots.new(id_type='OBJECT', name=f"Slot_{armature.name}")
                        channelbag = anim_utils.action_ensure_channelbag_for_slot(action, slot)
                        curve = channelbag.fcurves.new(path, index=i)
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

        # curve anim data
        if self.options.import_curves:
            if (mesh := get_armature_mesh(armature)) and (shape_keys := mesh.data.shape_keys):
                shape_keys.name = "Pose Asset"
                if shape_key_anim_data := shape_keys.animation_data:
                    shape_key_anim_data.action = None
    
                shape_keys_action = bpy.data.actions.new(name=f"{name}_Curves")
                
                if self.options.link:
                    shape_keys.animation_data_create()
                    shape_keys.animation_data.action = shape_keys_action
    
                key_blocks = shape_keys.key_blocks
                for key_block in key_blocks:
                    key_block.value = 0
                    
                for curve in data.curves:
                    
                    if not (shape_key := best(key_blocks, lambda block: block.name.lower(), curve.name.lower())):
                        continue
                        
                    for key in curve.keys:
                        shape_key.value = key.value
                        shape_key.keyframe_insert(data_path="value", frame=key.frame)

        return action, data

    def import_uepose_data(self, ar: FArchiveReader, name: str):
        assert isinstance(self.options, UEPoseOptions)  # noqa: S101
        
        data = UEPose.from_archive(ar)

        selected_armature = self.options.override_skeleton or get_active_armature()
        assert isinstance(selected_armature, bpy.types.Object)  # noqa: S101
        
        selected_mesh = get_armature_mesh(selected_armature)

        original_shape_key_lock = selected_mesh.show_only_shape_key
        original_mode = bpy.context.active_object.mode
        bpy.ops.object.mode_set(mode="OBJECT")
        armature_modifier: bpy.types.ArmatureModifier = first(
            selected_mesh.modifiers, lambda mod: mod.type == "ARMATURE"
        )

        selected_mesh.show_only_shape_key = False
        
        bone_swap_orig_parents(selected_armature)
        muted_constraints = disable_constraints(selected_armature)

        if not selected_mesh.data.shape_keys:
            # Create Basis shape key
            selected_mesh.shape_key_add(name="Basis", from_mix=False)


        # Store original shape key values so they aren't included in PoseAsset keys
        original_values = {}
        for shape_key in selected_mesh.data.shape_keys.key_blocks:
            if shape_key.value != 0:
                original_values[shape_key.name] = shape_key.value
                shape_key.value = 0
                
        root_bone = selected_armature.pose.bones.get(self.options.root_bone) or selected_armature.pose.bones[0]

        pose_names = []
        for pose in data.poses:
            pose_names.append(pose.name)

            # Enter pose mode
            bpy.context.view_layer.objects.active = selected_armature
            bpy.ops.object.mode_set(mode="POSE")

            # Reset all transforms to default
            bpy.ops.pose.select_all(action="SELECT")
            bpy.ops.pose.transforms_clear()
            bpy.ops.pose.select_all(action="DESELECT")

            # Move bones accordingly
            contributed = False
            for pose_key in pose.keys:

                pose_bone: bpy.types.PoseBone = get_case_insensitive(
                    selected_armature.pose.bones, pose_key.bone_name
                )
                
                if not pose_bone:
                    continue

                if root_bone and not bone_has_parent(pose_bone, root_bone):
                    continue

                # Verify that the current bone and all of its children
                # have at least one vertex group associated with it
                if not bone_hierarchy_has_vertex_groups(
                        pose_bone, selected_mesh.vertex_groups
                ):
                    continue

                # Reset bone to identity
                pose_bone.matrix_basis.identity()

                rotation = pose_key.rotation
                edit_bone = pose_bone.bone
                post_quat = (
                    Quaternion(post_quat)
                    if (post_quat := edit_bone.get("post_quat"))
                    else Quaternion()
                )

                q = post_quat.copy()
                q.rotate(make_quat(rotation))
                quat = post_quat.copy()
                quat.rotate(q.conjugated())
                pose_bone.rotation_quaternion = (
                        quat.conjugated() @ pose_bone.rotation_quaternion
                )

                loc = make_vector(
                    pose_key.position
                )
                loc.rotate(post_quat.conjugated())

                pose_bone.location = pose_bone.location + loc
                pose_bone.scale = Vector((1, 1, 1)) + make_vector(pose_key.scale)

                pose_bone.rotation_quaternion.normalize()
                contributed = True

            # Do not create shape keys if nothing changed
            if not contributed:
                continue

            # Create blendshape from armature
            bpy.ops.object.mode_set(mode="OBJECT")
            bpy.context.view_layer.objects.active = selected_mesh
            selected_mesh.select_set(True)
            bpy.ops.object.modifier_apply_as_shapekey(
                keep_modifier=True, modifier=armature_modifier.name
            )

            # Use name from pose data
            selected_mesh.data.shape_keys.key_blocks[-1].name = pose.name
            selected_mesh.data.shape_keys.key_blocks[-1].value = 0

        bpy.ops.object.mode_set(mode="OBJECT")
        bpy.context.view_layer.objects.active = selected_mesh
        selected_mesh.select_set(True)

        # create shape keys from curve data
        key_blocks = selected_mesh.data.shape_keys.key_blocks
        for pose in data.poses:
            if len(pose.curves) == 0:
                continue

            pose_name = pose.name
            if pose_name in key_blocks:
                pose_name = f"curve_{pose_name}"
            
            contributed = False
            for curve in pose.curves:
                target_curve_name = data.curve_names[curve.curve_index]
                if not (curve_shape_key := key_blocks.get(target_curve_name)):
                    continue

                curve_value = curve.influence
                if curve_value < curve_shape_key.slider_min:
                    curve_shape_key.slider_min = curve_value - 1.0

                if curve_value > curve_shape_key.slider_max:
                    curve_shape_key.slider_max = curve_value + 1.0

                curve_shape_key.value = curve_value
                contributed = True

            if contributed:
                selected_mesh.shape_key_add(name=pose_name, from_mix=True)
            
            for key in key_blocks:
                key.value = 0
                
        # Reset shape keys back to original values
        if len(original_values) > 0:
            for key_block in key_blocks:
                if orig_value := original_values.get(key_block.name):
                    key_block.value = orig_value
            
        # Final reset before re-entering regular import mode.
        bpy.context.view_layer.objects.active = selected_armature
        bpy.ops.object.mode_set(mode="POSE")
        bpy.ops.pose.select_all(action="SELECT")
        bpy.ops.pose.transforms_clear()
        bpy.ops.pose.select_all(action="DESELECT")

        bone_swap_orig_parents(selected_armature)
        for constraint in muted_constraints:
            constraint.mute = False

        selected_mesh.show_only_shape_key = original_shape_key_lock
        bpy.ops.object.mode_set(mode=original_mode)
        bpy.context.view_layer.objects.active = selected_mesh
        
    
