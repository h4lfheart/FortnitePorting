from __future__ import annotations

import bpy
from bpy.types import Action, FCurve, PoseBone
from bpy_extras import anim_utils
from mathutils import Quaternion, Vector

from ...options import UEAnimOptions
from ..dto.anim import AnimDto, QuatKeyDto, VectorKeyDto
from ..utils import best, get_active_armature, get_armature_mesh, get_case_insensitive


def _key_vector(key: VectorKeyDto) -> Vector:
    return Vector(key.value)


def _key_quat(key: QuatKeyDto) -> Quaternion:
    return Quaternion((key.value[3], key.value[0], key.value[1], key.value[2]))


def create_anim(dto: AnimDto, options: UEAnimOptions, name: str) -> Action:
    action = bpy.data.actions.new(name=name)

    armature = options.override_skeleton or get_active_armature()
    assert isinstance(armature, bpy.types.Object)

    if armature_anim_data := armature.animation_data:
        armature_anim_data.action = None

    if options.link:
        armature.animation_data_create()
        armature.animation_data.action = action

    if options.link and bpy.app.version >= (4, 4, 0):
        slot = action.slots.new(id_type="OBJECT", name=f"Slot_{armature.name}")
        armature.animation_data.action_slot = slot

    pose_bones = armature.pose.bones
    for track in dto.tracks:
        bone = get_case_insensitive(pose_bones, track.name)
        if bone is None:
            continue

        def create_fcurves(data_path: str, count: int, key_count: int, bone: PoseBone) -> list[FCurve]:
            path = bone.path_from_id(data_path)
            curves: list[FCurve] = []
            for i in range(count):
                if bpy.app.version < (5, 0, 0):
                    curve = action.fcurves.new(path, index=i)
                else:
                    slot = (
                        action.slots[0]
                        if len(action.slots) > 0
                        else action.slots.new(id_type="OBJECT", name=f"Slot_{armature.name}")
                    )
                    channelbag = anim_utils.action_ensure_channelbag_for_slot(action, slot)
                    curve = channelbag.fcurves.new(path, index=i)
                curve.keyframe_points.add(key_count)
                curves.append(curve)
            return curves

        def add_key(curves: list[FCurve], vector: Vector | Quaternion, key_index: int, frame: int) -> None:
            for i in range(len(vector)):
                curves[i].keyframe_points[key_index].co = frame, vector[i]
                curves[i].keyframe_points[key_index].interpolation = "LINEAR"

        orig_loc = Vector(orig_loc) if (orig_loc := bone.bone.get("orig_loc")) else Vector()
        orig_quat = Quaternion(orig_quat) if (orig_quat := bone.bone.get("orig_quat")) else Quaternion()
        post_quat = Quaternion(post_quat) if (post_quat := bone.bone.get("post_quat")) else Quaternion()

        if not options.rotation_only:
            loc_curves = create_fcurves("location", 3, len(track.position_keys), bone)
            scale_curves = create_fcurves("scale", 3, len(track.scale_keys), bone)
            for index, key in enumerate(track.position_keys):
                pos = _key_vector(key)
                pos -= orig_loc
                pos.rotate(post_quat.conjugated())
                add_key(loc_curves, pos, index, key.frame)
            for index, key in enumerate(track.scale_keys):
                add_key(scale_curves, Vector(key.value), index, key.frame)

        rot_curves = create_fcurves("rotation_quaternion", 4, len(track.rotation_keys), bone)
        for index, key in enumerate(track.rotation_keys):
            p_quat = _key_quat(key).conjugated()
            q = post_quat.copy()
            q.rotate(orig_quat)
            quat = q
            q = post_quat.copy()
            q.rotate(p_quat)
            quat.rotate(q.conjugated())
            add_key(rot_curves, quat, index, key.frame)

        bone.matrix_basis.identity()

    if options.import_curves:
        mesh = get_armature_mesh(armature)
        if mesh and (shape_keys := mesh.data.shape_keys):
            shape_keys.name = "Pose Asset"
            if shape_key_anim_data := shape_keys.animation_data:
                shape_key_anim_data.action = None

            shape_keys_action = bpy.data.actions.new(name=f"{name}_Curves")
            if options.link:
                shape_keys.animation_data_create()
                shape_keys.animation_data.action = shape_keys_action

            key_blocks = shape_keys.key_blocks
            for key_block in key_blocks:
                key_block.value = 0

            for curve in dto.curves:
                shape_key = best(key_blocks, lambda block: block.name.lower(), curve.name.lower())
                if not shape_key:
                    continue
                for key in curve.keys:
                    shape_key.value = key.value
                    shape_key.keyframe_insert(data_path="value", frame=key.frame)

    return action
