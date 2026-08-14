from __future__ import annotations

import bpy
from mathutils import Quaternion, Vector

from ...options import UEPoseOptions
from ..dto.pose import PoseDto
from ..utils import (
    bone_has_parent,
    bone_hierarchy_has_vertex_groups,
    bone_swap_orig_parents,
    disable_constraints,
    first,
    get_active_armature,
    get_armature_mesh,
    get_case_insensitive,
    make_quat,
    make_vector,
)


def create_pose(dto: PoseDto, options: UEPoseOptions, name: str):
    selected_armature = options.override_skeleton or get_active_armature()
    assert isinstance(selected_armature, bpy.types.Object)

    selected_mesh = get_armature_mesh(selected_armature)
    original_shape_key_lock = selected_mesh.show_only_shape_key
    original_mode = bpy.context.active_object.mode
    bpy.ops.object.mode_set(mode="OBJECT")
    armature_modifier = first(selected_mesh.modifiers, lambda mod: mod.type == "ARMATURE")

    selected_mesh.show_only_shape_key = False
    bone_swap_orig_parents(selected_armature)
    muted_constraints = disable_constraints(selected_armature)

    if not selected_mesh.data.shape_keys:
        selected_mesh.shape_key_add(name="Basis", from_mix=False)

    original_values = {}
    for shape_key in selected_mesh.data.shape_keys.key_blocks:
        if shape_key.value != 0:
            original_values[shape_key.name] = shape_key.value
            shape_key.value = 0

    root_bone = selected_armature.pose.bones.get(options.root_bone) or selected_armature.pose.bones[0]

    for pose in dto.poses:
        bpy.context.view_layer.objects.active = selected_armature
        bpy.ops.object.mode_set(mode="POSE")
        bpy.ops.pose.select_all(action="SELECT")
        bpy.ops.pose.transforms_clear()
        bpy.ops.pose.select_all(action="DESELECT")

        contributed = False
        for pose_key in pose.keys:
            pose_bone = get_case_insensitive(selected_armature.pose.bones, pose_key.bone_name)
            if not pose_bone:
                continue
            if root_bone and not bone_has_parent(pose_bone, root_bone):
                continue
            if not bone_hierarchy_has_vertex_groups(pose_bone, selected_mesh.vertex_groups):
                continue

            pose_bone.matrix_basis.identity()
            edit_bone = pose_bone.bone
            post_quat = Quaternion(post_quat) if (post_quat := edit_bone.get("post_quat")) else Quaternion()

            q = post_quat.copy()
            q.rotate(make_quat(pose_key.rotation))
            quat = post_quat.copy()
            quat.rotate(q.conjugated())
            pose_bone.rotation_quaternion = quat.conjugated() @ pose_bone.rotation_quaternion

            loc = make_vector(pose_key.position)
            loc.rotate(post_quat.conjugated())
            pose_bone.location = pose_bone.location + loc
            pose_bone.scale = Vector((1, 1, 1)) + make_vector(pose_key.scale)
            pose_bone.rotation_quaternion.normalize()
            contributed = True

        if not contributed:
            continue

        bpy.ops.object.mode_set(mode="OBJECT")
        bpy.context.view_layer.objects.active = selected_mesh
        selected_mesh.select_set(True)
        bpy.ops.object.modifier_apply_as_shapekey(keep_modifier=True, modifier=armature_modifier.name)
        selected_mesh.data.shape_keys.key_blocks[-1].name = pose.name
        selected_mesh.data.shape_keys.key_blocks[-1].value = 0

    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.context.view_layer.objects.active = selected_mesh
    selected_mesh.select_set(True)

    key_blocks = selected_mesh.data.shape_keys.key_blocks
    for pose in dto.poses:
        if len(pose.curves) == 0:
            continue

        pose_name = pose.name
        if pose_name in key_blocks:
            pose_name = f"curve_{pose_name}"

        contributed = False
        for curve in pose.curves:
            target_curve_name = dto.curve_names[curve.curve_index]
            curve_shape_key = key_blocks.get(target_curve_name)
            if not curve_shape_key:
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

    if original_values:
        for key_block in key_blocks:
            if orig_value := original_values.get(key_block.name):
                key_block.value = orig_value

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
