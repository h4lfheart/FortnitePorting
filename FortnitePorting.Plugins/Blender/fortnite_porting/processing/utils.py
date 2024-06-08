import bpy
import re
from .enums import *
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

def create_collection(name):
    if name in bpy.context.view_layer.layer_collection.children:
        bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children.get(name)
        return
    bpy.ops.object.select_all(action='DESELECT')

    new_collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(new_collection)
    bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children.get(
        new_collection.name)
    return new_collection

def get_armature_mesh(obj):
    if obj.type == 'ARMATURE' and len(obj.children) > 0:
        return obj.children[0]

    if obj.type == 'MESH':
        return obj

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

def make_vector(data, unreal_coords_correction=False):
    return Vector((data.get("X"), data.get("Y") * (-1 if unreal_coords_correction else 1), data.get("Z")))


def make_quat(data):
    return Quaternion((-data.get("W"), data.get("X"), -data.get("Y"), data.get("Z")))


def make_euler(data):
    return Euler((radians(data.get("Roll")), -radians(data.get("Pitch")), -radians(data.get("Yaw"))))