import bpy
from mathutils import Matrix, Quaternion, Vector
from bpy.types import Armature, EditBone
from typing import cast, Optional
from .utils import make_axis_vector


def is_skeleton_reoriented(armature_data: Armature) -> bool:
    if not armature_data.bones:
        return False

    for bone in armature_data.bones:
        if bone.get("is_socket"):
            continue
        return "reorient_direction" in bone

    return False


def has_reorient_data(armature_data: Armature) -> bool:
    if not armature_data.bones:
        return False

    sample_bone = armature_data.bones[0]
    return "orig_loc" in sample_bone and "orig_quat" in sample_bone


def reorient_bones(
        armature_data: Armature,
        bone_length: float = 1.0,
        allowed_reorient_children: Optional[dict[str, list[str]]] = None
) -> None:
    if not has_reorient_data(armature_data):
        raise RuntimeError("Armature bones missing required 'orig_loc' and 'orig_quat' properties")

    edit_bones = armature_data.edit_bones

    for bone in edit_bones:
        if bone.get("is_socket"):
            continue

        children = []
        for child in bone.children:
            if child.get("is_socket"):
                continue
            children.append(child)

        if len(children) == 0 and bone.parent is None:
            continue

        target_length = bone_length

        if len(children) == 0:
            new_rot = Vector(bone.parent["reorient_direction"])
            new_rot.rotate(Quaternion(bone["orig_quat"]).conjugated())
            target_rotation = make_axis_vector(new_rot)
        else:
            avg_child_pos = Vector()
            avg_child_length = 0.0
            allowed_children = None

            if allowed_reorient_children is not None:
                allowed_children = allowed_reorient_children.get(bone.name)

            for child in children:
                if allowed_children is not None and child.name not in allowed_children:
                    continue

                pos = Vector(child["orig_loc"])
                avg_child_pos += pos
                avg_child_length += pos.length

            avg_child_pos /= len(children)
            avg_child_length /= len(children)

            target_rotation = make_axis_vector(avg_child_pos)
            bone["reorient_direction"] = target_rotation
            target_length = avg_child_length

        post_quat = Vector((0, 1, 0)).rotation_difference(target_rotation)
        bone.matrix @= post_quat.to_matrix().to_4x4()
        bone.length = max(0.01, target_length)

        post_quat.rotate(Quaternion(bone["orig_quat"]).conjugated())
        bone["post_quat"] = post_quat

def reset_bone_orientation(armature_data: Armature, bone_length: float = 1.0) -> None:
    if not has_reorient_data(armature_data):
        raise RuntimeError("Armature bones missing required 'orig_loc' and 'orig_quat' properties")

    edit_bones = armature_data.edit_bones

    for bone in edit_bones:
        if bone.get("is_socket"):
            continue

        bone_pos = Vector(bone["orig_loc"])
        bone_rot = Quaternion(bone["orig_quat"])

        if "reorient_direction" in bone:
            del bone["reorient_direction"]

        # restore post_quat to non-reoriented state
        bone["post_quat"] = bone_rot.conjugated()

        bone.length = bone_length

        # Rebuild bone matrix from original data
        bone_matrix = Matrix.Translation(bone_pos) @ bone_rot.to_matrix().to_4x4()

        if bone.parent:
            bone_matrix = cast(Matrix, bone.parent.matrix) @ bone_matrix

        bone.matrix = bone_matrix