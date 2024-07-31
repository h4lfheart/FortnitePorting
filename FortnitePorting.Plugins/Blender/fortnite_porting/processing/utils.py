import bpy
import re

from .enums import *
from ..utils import *
from math import radians
from mathutils import Matrix, Vector, Euler, Quaternion

def merge_armatures(parts):
    bpy.ops.object.select_all(action='DESELECT')

    merge_parts = []
    constraint_parts = []

    for part in parts:
        if (meta := part.get("Meta")) and meta.get("AttachToSocket") and meta.get("Socket") not in ["Face", "Helmet", None]:
            constraint_parts.append(part)
        else:
            merge_parts.append(part)

    # merge skeletons
    for part in merge_parts:
        data = part.get("Data")
        mesh_type = part.get("Type")
        skeleton = part.get("Skeleton")

        if mesh_type == EFortCustomPartType.BODY:
            bpy.context.view_layer.objects.active = skeleton

        skeleton.select_set(True)

    bpy.ops.object.join()
    master_skeleton = bpy.context.active_object
    master_mesh = get_armature_mesh(bpy.context.active_object)
    bpy.ops.object.select_all(action='DESELECT')

    # merge meshes
    for part in merge_parts:
        data = part.get("Data")
        mesh_type = part.get("Type")
        mesh = part.get("Mesh")

        if mesh_type == EFortCustomPartType.BODY:
            bpy.context.view_layer.objects.active = mesh

        mesh.select_set(True)

    bpy.ops.object.join()
    bpy.ops.object.select_all(action='DESELECT')

    # rebuild master bone tree
    bone_tree = {}
    for bone in master_skeleton.data.bones:
        try:
            bone_reg = re.sub(".\d\d\d", "", bone.name)
            parent_reg = re.sub(".\d\d\d", "", bone.parent.name)
            bone_tree[bone_reg] = parent_reg
        except AttributeError:
            pass

    bpy.context.view_layer.objects.active = master_skeleton
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.armature.select_all(action='DESELECT')
    bpy.ops.object.select_pattern(pattern="*.[0-9][0-9][0-9]")
    bpy.ops.armature.delete()

    skeleton_bones = master_skeleton.data.edit_bones

    for bone, parent in bone_tree.items():
        if target_bone := skeleton_bones.get(bone):
            target_bone.parent = skeleton_bones.get(parent)

    bpy.ops.object.mode_set(mode='OBJECT')

    # constraint meshes
    for part in constraint_parts:
        skeleton = part.get("Skeleton")
        meta = part.get("Meta")
        socket = meta.get("Socket")
        if socket is None:
            return

        if socket.casefold() == "hat":
            socket = "head"

        constraint_object(skeleton, master_skeleton, socket, [0, radians(90), 0])  # TODO makes this proper for v3

    return master_skeleton

def create_or_get_collection(name):
    if existing_collection := bpy.data.collections.get(name):
        bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children.get(name)
        return existing_collection
    
    bpy.ops.object.select_all(action='DESELECT')
    
    new_collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(new_collection)
    bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children.get(name)
    return new_collection

def get_armature_mesh(obj):
    if obj.type == 'ARMATURE' and len(obj.children) > 0:
        return obj.children[0]

    if obj.type == 'MESH':
        return obj
    
def get_selected_armature():
    selected = bpy.context.active_object
    if selected.type == 'ARMATURE':
        return selected
    elif selected.type == 'MESH':
        armature_modifier = first(selected.modifiers, lambda modifier: modifier.type == 'ARMATURE')
        return armature_modifier.object
    else:
        return None

def constraint_object(child: bpy.types.Object, parent: bpy.types.Object, bone: str, rot):
    constraint = child.constraints.new('CHILD_OF')
    constraint.target = parent
    constraint.subtarget = bone
    child.rotation_mode = 'XYZ'
    child.rotation_euler = rot
    constraint.inverse_matrix = Matrix()

def bone_hierarchy_has_vertex_groups(bone, vertex_groups):
    if bone.name in vertex_groups:
        return True
    children = bone.children_recursive
    for child in children:
        if child.name in vertex_groups:
            return True
    return False

def bone_has_parent(child, parent):
    if child == parent:
        return True
    return parent in child.parent_recursive

def make_vector(data, unreal_coords_correction=False):
    return Vector((data.get("X"), data.get("Y") * (-1 if unreal_coords_correction else 1), data.get("Z")))


def make_quat(data):
    return Quaternion((-data.get("W"), data.get("X"), -data.get("Y"), data.get("Z")))


def make_euler(data):
    return Euler((radians(data.get("Roll")), -radians(data.get("Pitch")), -radians(data.get("Yaw"))))

def time_to_frame(time, fps = 30):
    return int(round(time * fps))

def clear_children_bone_transforms(skeleton, anim, bone_name):
    bpy.ops.object.mode_set(mode='POSE')
    bpy.ops.pose.select_all(action='DESELECT')
    pose_bones = skeleton.pose.bones
    bones = skeleton.data.bones
    if target_bone := first(bones, lambda x: x.name == bone_name):
        target_bones = target_bone.children_recursive
        target_bones.append(target_bone)
        dispose_paths = []
        for bone in target_bones:
            dispose_paths.append(f'pose.bones["{bone.name}"].rotation_quaternion')
            dispose_paths.append(f'pose.bones["{bone.name}"].location')
            dispose_paths.append(f'pose.bones["{bone.name}"].scale')
            pose_bones[bone.name].matrix_basis = Matrix()
        dispose_curves = [fcurve for fcurve in anim.fcurves if fcurve.data_path in dispose_paths]
        for fcurve in dispose_curves:
            anim.fcurves.remove(fcurve)
    bpy.ops.object.mode_set(mode='OBJECT')
