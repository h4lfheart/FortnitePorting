import bpy
import re

from .enums import *
from ..utils import *
from math import radians
from mathutils import Matrix, Vector, Euler, Quaternion
from bpy_extras import anim_utils

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

    # move curve blendshapes to bottom if there are any
    if master_mesh := get_armature_mesh(bpy.context.active_object):
        if master_mesh.data.shape_keys is not None:
            for shape_key in list(master_mesh.data.shape_keys.key_blocks):
                if not shape_key.name.startswith('curves_'):
                    continue
                master_mesh.active_shape_key_index = master_mesh.data.shape_keys.key_blocks.find(shape_key.name)
                bpy.ops.object.shape_key_move(type='BOTTOM')
            master_mesh.active_shape_key_index = 0

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

        # Account for skins with lowercase tail socket bone
        if socket == "Tail" and "tail" in master_skeleton.pose.bones:
            socket = "tail"

        constraint_object(skeleton, master_skeleton, socket, [0, 0, 0], rot=False)
        constraint_object(skeleton, master_skeleton, socket, [0, 0, 0], loc=False, scale=False, use_inverse=True)

    return master_skeleton

def create_or_get_collection(name):
    fixed_name = name[:63]
    if existing_collection := bpy.data.collections.get(fixed_name):
        bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children.get(fixed_name)
        return existing_collection
    
    bpy.ops.object.select_all(action='DESELECT')
    
    new_collection = bpy.data.collections.new(fixed_name)
    bpy.context.scene.collection.children.link(new_collection)
    bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children.get(fixed_name)
    return new_collection

def get_armature_mesh(obj):
    if obj.type == 'ARMATURE' and len(obj.children) > 0:
        return first(obj.children, lambda child: child.type == 'MESH')

    if obj.type == 'MESH':
        return obj
    return None
    
def get_selected_armature():
    if not (selected := bpy.context.active_object):
        return None
        
    if selected.type == 'ARMATURE':
        return selected
    elif selected.type == 'MESH':
        armature_modifier = first(selected.modifiers, lambda modifier: modifier.type == 'ARMATURE')
        return armature_modifier.object if armature_modifier is not None else None
    else:
        return None

def disable_constraints(armature: bpy.types.Object) -> list[bpy.types.Constraint]:
    constraints_muted = []
    for bone in armature.pose.bones:
        for constraint in bone.constraints:
            if not constraint.mute:
                constraint.mute = True
                constraints_muted.append(constraint)
    return constraints_muted

def constraint_object(child: bpy.types.Object, parent: bpy.types.Object, bone: str, rotation, loc=True, rot=True, scale=True, use_inverse=False):
    constraint = child.constraints.new('CHILD_OF')
    constraint.target = parent
    constraint.subtarget = bone
    child.rotation_mode = 'XYZ'
    child.rotation_euler = rotation
    
    constraint.use_location_x = loc
    constraint.use_location_y = loc
    constraint.use_location_z = loc

    constraint.use_rotation_x = rot
    constraint.use_rotation_y = rot
    constraint.use_rotation_z = rot
    
    constraint.use_scale_x = scale
    constraint.use_scale_y = scale
    constraint.use_scale_z = scale
    
    if use_inverse:
        try:
            with bpy.context.temp_override(active_object=child):
                bpy.ops.constraint.childof_set_inverse(constraint=constraint.name, owner='OBJECT')
        except RuntimeError:
            pass
    else:
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

def bone_roll(bone: bpy.types.EditBone, roll: float):
    assert 'orig_roll' not in bone
    bone['orig_roll'] = bone.roll
    bone.roll = roll

def bone_tail(bone: bpy.types.EditBone, tail):
    assert 'orig_tail' not in bone
    bone['orig_tail'] = bone.tail
    bone.tail = tail

def bone_head(bone: bpy.types.EditBone, head):
    assert 'orig_head' not in bone
    bone['orig_head'] = bone.head
    bone.head = head

def bone_parent(child: bpy.types.EditBone, parent_to: bpy.types.EditBone):
    assert child != parent_to

    # Save original parent as a custom property
    if child.parent:
        assert 'orig_parent' not in child
        child['orig_parent'] = child.parent.name

    child.parent = parent_to

def bone_swap_properties(all_bones: bpy.types.ArmatureEditBones, bone: bpy.types.EditBone):
    if orig_roll := bone.get('orig_roll'):
        curr_roll = bone.roll
        bone.roll = orig_roll
        bone['orig_roll'] = curr_roll

    if orig_tail := bone.get('orig_tail'):
        curr_tail = bone.tail.copy()
        bone.tail = orig_tail
        bone['orig_tail'] = curr_tail

    if orig_head := bone.get('orig_head'):
        curr_head = bone.head.copy()
        bone.head = orig_head
        bone['orig_head'] = curr_head

    if orig_parent := bone.get('orig_parent'):
        curr_bone_parent_name = bone.parent.name
        bone.parent = all_bones.get(orig_parent)
        bone['orig_parent'] = curr_bone_parent_name

def bone_swap_orig_parents(armature_obj: bpy.types.Object):
    original_mode = bpy.context.active_object.mode
    original_selected_object = bpy.context.active_object
    try:
        bpy.context.view_layer.objects.active = armature_obj
        edit_bones = armature_obj.data.edit_bones
        bpy.ops.object.mode_set(mode='EDIT')
        # Iterate all edit bones and reparent back to 'orig_parent' if property exists and make note of existing parent.
        for edit_bone in edit_bones:
            bone_swap_properties(edit_bones, edit_bone)
    finally:
        bpy.ops.object.mode_set(mode=original_mode)
        bpy.context.view_layer.objects.active = original_selected_object

def make_vector(data, unreal_coords_correction=False):
    return Vector((data.get("X"), data.get("Y") * (-1 if unreal_coords_correction else 1), data.get("Z")))


def make_quat(data):
    return Quaternion((-data.get("W"), data.get("X"), -data.get("Y"), data.get("Z")))


def make_euler(data):
    return Euler((radians(data.get("Roll")), -radians(data.get("Pitch")), -radians(data.get("Yaw"))))

def time_to_frame(time, fps = 30):
    return int(round(time * fps))

def clear_children_bone_transforms(skeleton, anim, bone_name):
    bpy.context.view_layer.objects.active = skeleton
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
            
        if len(anim.slots) > 0:
            channelbag = anim_utils.action_ensure_channelbag_for_slot(anim, anim.slots[0])
            dispose_curves = [fcurve for fcurve in channelbag.fcurves if fcurve.data_path in dispose_paths]
            for fcurve in dispose_curves:
                channelbag.fcurves.remove(fcurve)
    bpy.ops.object.mode_set(mode='OBJECT')

def set_geo_nodes_param(geo_node_modifier, name, value):
    identifier = geo_node_modifier.node_group.interface.items_tree[name].identifier
    geo_node_modifier[identifier] = value
    
    
def get_sequence_editor():
    if not bpy.context.workspace.sequencer_scene:
        bpy.context.workspace.sequencer_scene = bpy.context.scene
        
    seq_scene = bpy.context.workspace.sequencer_scene
    
    if not seq_scene.sequence_editor:
        seq_scene.sequence_editor_create()
        
    return seq_scene.sequence_editor