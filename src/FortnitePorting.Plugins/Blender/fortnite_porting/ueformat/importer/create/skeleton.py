from __future__ import annotations

from typing import cast

import bpy
from bpy.types import EditBone, Object
from mathutils import Matrix, Quaternion, Vector

from ...options import UEModelOptions
from ..dto.model import ModelDto
from ..dto.skeleton import SkeletonDto
from ..reorient_utils import reorient_bones
from ..utils import get_case_insensitive, get_weighted_vertex_group_indices, has_vertex_weights


def _xyzw_to_quat(rotation) -> Quaternion:
    return Quaternion((rotation[3], rotation[0], rotation[1], rotation[2]))


def create_skeleton(
    dto: ModelDto,
    options: UEModelOptions,
    name: str,
    created_lods: list[Object],
) -> Object | None:
    skeleton = dto.skeleton
    if skeleton is None:
        return None
    if not (skeleton.bones or (options.import_sockets and skeleton.sockets)):
        return None

    armature_data = bpy.data.armatures.new(name=name)
    armature_data.display_type = "STICK"

    template_armature_object = bpy.data.objects.new(name + "_Template_Skeleton", armature_data)
    template_armature_object.show_in_front = True
    bpy.context.collection.objects.link(template_armature_object)
    bpy.context.view_layer.objects.active = template_armature_object
    template_armature_object.select_set(state=True)

    if skeleton.bones:
        bpy.ops.object.mode_set(mode="EDIT")
        edit_bones = armature_data.edit_bones
        for bone_ in skeleton.bones:
            bone_pos = Vector(bone_.position)
            bone_rot = _xyzw_to_quat(bone_.rotation)
            bone_scale = Vector(bone_.scale)

            edit_bone = edit_bones.new(bone_.name)
            edit_bone["orig_loc"] = bone_pos
            edit_bone["orig_quat"] = bone_rot.conjugated()
            edit_bone.length = options.bone_length * options.scale_factor

            bone_matrix = Matrix.LocRotScale(bone_pos, bone_rot, bone_scale)

            if bone_.parent_index >= 0:
                parent_bone = cast(
                    EditBone | None,
                    edit_bones.get(skeleton.bones[bone_.parent_index].name),
                )
                assert parent_bone
                edit_bone.parent = parent_bone
                bone_matrix = cast(Matrix, parent_bone.matrix) @ bone_matrix

            edit_bone.matrix = bone_matrix

            if not options.reorient_bones:
                edit_bone["post_quat"] = bone_rot

        bpy.ops.object.mode_set(mode="OBJECT")

    if options.import_sockets and skeleton.sockets:
        bpy.ops.object.mode_set(mode="EDIT")
        edit_bones = armature_data.edit_bones
        socket_collection = armature_data.collections.new("Sockets")
        for socket in skeleton.sockets:
            socket_bone = edit_bones.new(socket.name)
            socket_collection.assign(socket_bone)
            socket_bone["is_socket"] = True
            parent_bone = cast(
                EditBone | None,
                get_case_insensitive(edit_bones, socket.parent_name),
            )
            if parent_bone is None:
                continue
            socket_bone.parent = parent_bone
            socket_bone.length = options.bone_length * options.scale_factor
            socket_bone.matrix = (
                cast(Matrix, parent_bone.matrix)
                @ Matrix.Translation(socket.position)
                @ _xyzw_to_quat(socket.rotation).to_matrix().to_4x4()
            )
        bpy.ops.object.mode_set(mode="OBJECT")

    if skeleton.bones and options.reorient_bones:
        bpy.ops.object.mode_set(mode="EDIT")
        reorient_bones(
            armature_data,
            bone_length=options.bone_length * options.scale_factor,
            allowed_reorient_children=options.allowed_reorient_children,
        )
        bpy.ops.object.mode_set(mode="OBJECT")

    return_object = None
    if created_lods:
        bpy.data.objects.remove(template_armature_object)
        for lod in created_lods:
            return_object = _bind_lod(lod, armature_data, skeleton, options)
    else:
        template_armature_object.name = name
        return_object = template_armature_object
        _finalize_standalone_armature(template_armature_object, armature_data, skeleton, options)

    return return_object


def _finalize_standalone_armature(
    armature_object: Object,
    armature_data,
    skeleton: SkeletonDto,
    options: UEModelOptions,
) -> None:
    if options.import_virtual_bones:
        bpy.ops.object.mode_set(mode="EDIT")
        virtual_bone_collection = armature_data.collections.new("Virtual Bones")
        edit_bones = armature_data.edit_bones
        for virtual in skeleton.virtual_bones:
            source_bone = edit_bones.get(virtual.source_name)
            if source_bone is None:
                continue
            virtual_bone = edit_bones.new(virtual.virtual_name)
            virtual_bone_collection.assign(virtual_bone)
            virtual_bone.head = source_bone.tail
            virtual_bone.tail = source_bone.head

        bpy.ops.object.mode_set(mode="POSE")
        for virtual in skeleton.virtual_bones:
            virtual_bone = armature_object.pose.bones.get(virtual.virtual_name)
            if virtual_bone is None:
                continue
            constraint = virtual_bone.constraints.new("IK")
            constraint.target = armature_object
            constraint.subtarget = virtual.target_name
            constraint.chain_count = 1
            virtual_bone.ik_stretch = 1

        bpy.ops.object.mode_set(mode="OBJECT")

    _apply_bone_colors(armature_object, skeleton)


def _bind_lod(lod: Object, armature_data, skeleton: SkeletonDto, options: UEModelOptions) -> Object:
    armature_object = bpy.data.objects.new(lod.name + "_Skeleton", armature_data)
    armature_object.show_in_front = True

    if options.link:
        bpy.context.collection.objects.link(armature_object)
    bpy.context.view_layer.objects.active = armature_object
    armature_object.select_set(state=True)

    lod.parent = armature_object

    armature_modifier = lod.modifiers.new(armature_object.name, type="ARMATURE")
    armature_modifier.show_expanded = False
    armature_modifier.use_vertex_groups = True
    armature_modifier.object = armature_object

    bpy.ops.object.mode_set(mode="POSE")

    weighted_groups = get_weighted_vertex_group_indices(lod)
    for bone in armature_object.pose.bones:
        vertex_group = lod.vertex_groups.get(bone.name)
        if not vertex_group or not has_vertex_weights(lod, vertex_group, weighted_groups):
            bone.color.palette = "THEME14"
            continue
        if not bone.children:
            bone.color.palette = "THEME03"

    _apply_bone_colors(armature_object, skeleton, skip_leaf_and_unweighted=True)
    bpy.ops.object.mode_set(mode="OBJECT")
    return armature_object


def _apply_bone_colors(armature_object: Object, skeleton: SkeletonDto, skip_leaf_and_unweighted: bool = False) -> None:
    if not skip_leaf_and_unweighted:
        for bone in armature_object.pose.bones:
            if not bone.children:
                bone.color.palette = "THEME03"

    for socket in skeleton.sockets:
        socket_bone = armature_object.pose.bones.get(socket.name)
        if socket_bone is not None:
            socket_bone.color.palette = "THEME05"

    for virtual in skeleton.virtual_bones:
        virtual_bone = armature_object.pose.bones.get(virtual.virtual_name)
        if virtual_bone is not None:
            virtual_bone.color.palette = "THEME11"
