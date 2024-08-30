import bpy

from .import_context import *
from ..utils import *
from mathutils import Matrix, Vector, Euler, Quaternion

class Lazy:
    def __init__(self, getter):
        self.getter = getter

    def value(self):
        try:
            return self.getter()
        except Exception as e:
            return None

class TastyRigOptions:
    def __init__(self, scale: float = 0.01, master_skeleton=None):
        self.scale = scale
        self.master_skeleton = master_skeleton
        
class CustomShape:
    def __init__(self, bone_name, object_name, palette_name, wire_width=2.5, offset=(0.0, 0.0, 0.0), rotation=(0.0, 0.0, 0.0), scale=1.0, scale_to_bone_length=False):
        self.bone_name = bone_name
        self.object_name = object_name
        self.palette_name = palette_name
        self.wire_width = wire_width
        self.offset = offset
        self.rotation = rotation
        self.scale = scale
        self.scale_to_bone_length = scale_to_bone_length

class IKBone:
    def __init__(self, bone_name, target_name, pole_name, chain_length, pole_angle=180, use_rotation=False, lock_axes=None):
        self.bone_name = bone_name
        self.target_name = target_name
        self.pole_name = pole_name
        self.chain_length = chain_length
        self.pole_angle = pole_angle
        self.use_rotation = use_rotation
        self.lock_axes = lock_axes or []
        
        
def create_tasty_rig(context, target_skeleton, options: TastyRigOptions):
    armature_data = target_skeleton.data

    base_collection = armature_data.collections.new("Base")
    rig_collection = armature_data.collections.new("Rig")
    twist_collection = armature_data.collections.new("Twist")
    dynamic_collection = armature_data.collections.new("Dynamic")
    deform_collection = armature_data.collections.new("Deform")
    extra_collection = armature_data.collections.new("Extra")
    
    has_original_ik_bones = any(armature_data.bones, lambda bone: bone.name in ["ik_hand_root", "ik_foot_root"])
    if not has_original_ik_bones:
        mesh = context.get_metadata("MasterSkeletalMesh")
        master_skeletal_mesh = context.import_model(mesh)

        bpy.context.view_layer.objects.active = master_skeletal_mesh
        master_skeletal_mesh.select_set(True)
        
        bpy.ops.object.mode_set(mode='EDIT')
        bpy.ops.armature.select_all(action='DESELECT')
        bpy.ops.object.select_pattern(pattern="*ik_*")
        bpy.ops.armature.select_all(action='INVERT')
        bpy.ops.armature.delete()

        bpy.ops.object.mode_set(mode='OBJECT')
        bpy.context.view_layer.objects.active = target_skeleton
        target_skeleton.select_set(True)
        master_skeletal_mesh.select_set(True)
        bpy.ops.object.join()
    
    # bone creation and modification
    bpy.ops.object.mode_set(mode='EDIT')
    edit_bones = armature_data.edit_bones

    # new name, target name, parent name
    dupe_bones = [
        ("ik_hand_parent_r", "hand_r", "ik_hand_r"),
        ("ik_hand_parent_l", "hand_l", "ik_hand_l"),
        ("ik_hand_target_r", "hand_r", "ik_hand_parent_r"),
        ("ik_hand_target_l", "hand_l", "ik_hand_parent_l"),
        ("ik_foot_rot_ctrl_r", "foot_r", "ik_foot_target_r"),
        ("ik_foot_rot_ctrl_l", "foot_l", "ik_foot_target_l"),

        ("ik_finger_pole_thumb_r", "thumb_02_r", "ik_hand_target_r"),
        ("ik_finger_pole_index_r", "index_02_r", "ik_hand_target_r"),
        ("ik_finger_pole_middle_r", "middle_02_r", "ik_hand_target_r"),
        ("ik_finger_pole_ring_r", "ring_02_r", "ik_hand_target_r"),
        ("ik_finger_pole_pinky_r", "pinky_02_r", "ik_hand_target_r"),

        ("ik_finger_pole_thumb_l", "thumb_02_l", "ik_hand_target_l"),
        ("ik_finger_pole_index_l", "index_02_l", "ik_hand_target_l"),
        ("ik_finger_pole_middle_l", "middle_02_l", "ik_hand_target_l"),
        ("ik_finger_pole_ring_l", "ring_02_l", "ik_hand_target_l"),
        ("ik_finger_pole_pinky_l", "pinky_02_l", "ik_hand_target_l"),
    ]
            

    for new_name, target_name, parent_name in dupe_bones:
        if not (target_bone := edit_bones.get(target_name)): continue
        if not (parent_bone := edit_bones.get(parent_name)): continue

        bone = edit_bones.get(new_name) or edit_bones.new(new_name)
        bone.parent = parent_bone

        bone.head = target_bone.head
        bone.tail = target_bone.tail
        bone.roll = target_bone.roll

    # new name, parent name, (head, tail, roll)
    new_bones = [
        ("ik_foot_pole_r", "ik_foot_r", Lazy(lambda: (edit_bones["calf_r"].head + Vector((-0.05, -0.75, 0)), edit_bones["calf_r"].head + Vector((-0.05, -0.7, 0)), 0))),
        ("ik_foot_pole_l", "ik_foot_l", Lazy(lambda: (edit_bones["calf_l"].head + Vector((0.05, -0.75, 0)), edit_bones["calf_l"].head + Vector((0.05, -0.7, 0)), 0))),
        ("ik_hand_pole_r", "ik_hand_r", Lazy(lambda: (edit_bones["lowerarm_r"].head + Vector((0, 0.75, 0)), edit_bones["lowerarm_r"].head + Vector((0, 0.7, 0)), 0))),
        ("ik_hand_pole_l", "ik_hand_l", Lazy(lambda: (edit_bones["lowerarm_l"].head + Vector((0, 0.75, 0)), edit_bones["lowerarm_l"].head + Vector((0, 0.7, 0)), 0))),

        ("ik_foot_ctrl_r", "ik_foot_r", Lazy(lambda: (edit_bones["ball_r"].head + Vector((0, 0.2, 0)), edit_bones["ball_r"].tail + Vector((0, 0.2, 0)), edit_bones["ball_r"].roll))),
        ("ik_foot_roll_inner_r", "ik_foot_r", Lazy(lambda: (Vector((edit_bones["ball_r"].head.x + 0.04, edit_bones["ball_r"].head.y, 0)), Vector((edit_bones["ball_r"].tail.x + 0.04, edit_bones["ball_r"].tail.y, 0)), edit_bones["ball_r"].roll))),
        ("ik_foot_roll_outer_r", "ik_foot_roll_inner_r", Lazy(lambda: (Vector((edit_bones["ball_r"].head.x - 0.04, edit_bones["ball_r"].head.y, 0)), Vector((edit_bones["ball_r"].tail.x - 0.04, edit_bones["ball_r"].tail.y, 0)), edit_bones["ball_r"].roll))),
        ("ik_foot_roll_front_r", "ik_foot_roll_outer_r", Lazy(lambda: (edit_bones["ball_r"].head, edit_bones["ball_r"].tail, edit_bones["ball_r"].roll + radians(180)))),
        ("ik_foot_roll_back_r", "ik_foot_roll_front_r", Lazy(lambda: (Vector((edit_bones["foot_r"].head.x, edit_bones["foot_r"].head.y + 0.065, 0)), Vector((edit_bones["foot_r"].tail.x, edit_bones["foot_r"].tail.y + 0.065, 0)), edit_bones["ball_r"].roll))),
        ("ik_foot_target_r", "ik_foot_roll_back_r", Lazy(lambda: (edit_bones["ik_foot_r"].head, edit_bones["ik_foot_r"].tail, edit_bones["ik_foot_r"].roll))),

        ("ik_foot_ctrl_l", "ik_foot_l", Lazy(lambda: (edit_bones["ball_l"].head + Vector((0, 0.2, 0)), edit_bones["ball_l"].tail + Vector((0, 0.2, 0)), edit_bones["ball_l"].roll))),
        ("ik_foot_roll_inner_l", "ik_foot_l", Lazy(lambda: (Vector((edit_bones["ball_l"].head.x + 0.04, edit_bones["ball_l"].head.y, 0)), Vector((edit_bones["ball_l"].tail.x + 0.04, edit_bones["ball_l"].tail.y, 0)), edit_bones["ball_l"].roll))),
        ("ik_foot_roll_outer_l", "ik_foot_roll_inner_l", Lazy(lambda: (Vector((edit_bones["ball_l"].head.x - 0.04, edit_bones["ball_l"].head.y, 0)), Vector((edit_bones["ball_l"].tail.x - 0.04, edit_bones["ball_l"].tail.y, 0)), edit_bones["ball_l"].roll))),
        ("ik_foot_roll_front_l", "ik_foot_roll_outer_l", Lazy(lambda: (edit_bones["ball_l"].head, edit_bones["ball_l"].tail, edit_bones["ball_l"].roll + radians(180)))),
        ("ik_foot_roll_back_l", "ik_foot_roll_front_l", Lazy(lambda: (Vector((edit_bones["foot_l"].head.x, edit_bones["foot_l"].head.y + 0.065, 0)), Vector((edit_bones["foot_l"].tail.x, edit_bones["foot_l"].tail.y + 0.065, 0)), edit_bones["ball_l"].roll))),
        ("ik_foot_target_l", "ik_foot_roll_back_l", Lazy(lambda: (edit_bones["ik_foot_l"].head, edit_bones["ik_foot_l"].tail, edit_bones["ik_foot_l"].roll))),

        ("ik_finger_thumb_r", "ik_hand_target_r", Lazy(lambda: (edit_bones["thumb_03_r"].tail, 2 * edit_bones["thumb_03_r"].tail - edit_bones["thumb_03_r"].head, edit_bones["thumb_03_r"].roll))),
        ("ik_finger_index_r", "ik_hand_target_r", Lazy(lambda: (edit_bones["index_03_r"].tail, 2 * edit_bones["index_03_r"].tail - edit_bones["index_03_r"].head, edit_bones["index_03_r"].roll))),
        ("ik_finger_middle_r", "ik_hand_target_r", Lazy(lambda: (edit_bones["middle_03_r"].tail, 2 * edit_bones["middle_03_r"].tail - edit_bones["middle_03_r"].head, edit_bones["middle_03_r"].roll))),
        ("ik_finger_ring_r", "ik_hand_target_r", Lazy(lambda: (edit_bones["ring_03_r"].tail, 2 * edit_bones["ring_03_r"].tail - edit_bones["ring_03_r"].head, edit_bones["ring_03_r"].roll))),
        ("ik_finger_pinky_r", "ik_hand_target_r", Lazy(lambda: (edit_bones["pinky_03_r"].tail, 2 * edit_bones["pinky_03_r"].tail - edit_bones["pinky_03_r"].head, edit_bones["pinky_03_r"].roll))),
        
        ("ik_finger_thumb_l", "ik_hand_target_l", Lazy(lambda: (edit_bones["thumb_03_l"].tail, 2 * edit_bones["thumb_03_l"].tail - edit_bones["thumb_03_l"].head, edit_bones["thumb_03_l"].roll))),
        ("ik_finger_index_l", "ik_hand_target_l", Lazy(lambda: (edit_bones["index_03_l"].tail, 2 * edit_bones["index_03_l"].tail - edit_bones["index_03_l"].head, edit_bones["index_03_l"].roll))),
        ("ik_finger_middle_l", "ik_hand_target_l", Lazy(lambda: (edit_bones["middle_03_l"].tail, 2 * edit_bones["middle_03_l"].tail - edit_bones["middle_03_l"].head, edit_bones["middle_03_l"].roll))),
        ("ik_finger_ring_l", "ik_hand_target_l", Lazy(lambda: (edit_bones["ring_03_l"].tail, 2 * edit_bones["ring_03_l"].tail - edit_bones["ring_03_l"].head, edit_bones["ring_03_l"].roll))),
        ("ik_finger_pinky_l", "ik_hand_target_l", Lazy(lambda: (edit_bones["pinky_03_l"].tail, 2 * edit_bones["pinky_03_l"].tail - edit_bones["pinky_03_l"].head, edit_bones["pinky_03_l"].roll))),

    ]

    for new_name, parent_name, lazy in new_bones:
        if not (data_tuple := lazy.value()): continue
        if not (parent_bone := edit_bones.get(parent_name)): continue

        bone = edit_bones.get(new_name) or edit_bones.new(new_name)
        bone.parent = parent_bone

        head, tail, roll = data_tuple
        bone.head = head
        bone.tail = tail
        bone.roll = roll
        
    parent_bones = [
        #("ik_foot_r", "ik_foot_roll_back_r")
    ]
    
    for bone_name, target_name in parent_bones:
        if not (existing_bone := edit_bones.get(bone_name)): continue
        if not (target_bone := edit_bones.get(target_name)): continue
        
        existing_bone.parent = target_bone

    tail_adjustment_bones = [
        ("lowerarm_r", Lazy(lambda: edit_bones["ik_hand_r" if has_original_ik_bones else "hand_r"].head)),
        ("lowerarm_l", Lazy(lambda: edit_bones["ik_hand_l" if has_original_ik_bones else "hand_l"].head)),
        ("calf_r", Lazy(lambda: edit_bones["ik_foot_r" if has_original_ik_bones else "foot_r"].head)),
        ("calf_l", Lazy(lambda: edit_bones["ik_foot_l" if has_original_ik_bones else "foot_l"].head)),
    ]

    for bone_name, lazy in tail_adjustment_bones:
        if not (bone := edit_bones.get(bone_name)): continue
        if not (position := lazy.value()): continue
    
        bone.tail = position

    transform_adjustment_bones = [
        ("ik_finger_pole_thumb_r", Vector((0.05, 0.0, 0.0))),
        ("ik_finger_pole_index_r", Vector((0.05, 0.0, 0.0))),
        ("ik_finger_pole_middle_r", Vector((0.05, 0.0, 0.0))),
        ("ik_finger_pole_ring_r", Vector((0.05, 0.0, 0.0))),
        ("ik_finger_pole_pinky_r", Vector((0.05, 0.0, 0.0))),

        ("ik_finger_pole_thumb_l", Vector((0.05, 0.0, 0.0))),
        ("ik_finger_pole_index_l", Vector((0.05, 0.0, 0.0))),
        ("ik_finger_pole_middle_l", Vector((0.05, 0.0, 0.0))),
        ("ik_finger_pole_ring_l", Vector((0.05, 0.0, 0.0))),
        ("ik_finger_pole_pinky_l", Vector((0.05, 0.0, 0.0))),
    ]

    for name, transform in transform_adjustment_bones:
        if not (bone := edit_bones.get(name)): continue

        bone.matrix @= Matrix.Translation(transform)



    # pose bone modifications
    bpy.ops.object.mode_set(mode='POSE')

    pose_bones = target_skeleton.pose.bones

    def add_foot_ik_constraints(suffix):
        is_left = suffix == "l"
        ctrl_bone_name = f"ik_foot_ctrl_{suffix}"

        if inner_roll_bone := pose_bones.get(f"ik_foot_roll_inner_{suffix}"):
            copy_rotation = inner_roll_bone.constraints.new("COPY_ROTATION")
            copy_rotation.target = target_skeleton
            copy_rotation.subtarget = ctrl_bone_name
            copy_rotation.use_x = False
            copy_rotation.use_y = True
            copy_rotation.use_z = False
            copy_rotation.target_space = "LOCAL"
            copy_rotation.owner_space = "LOCAL"

            limit_rotation = inner_roll_bone.constraints.new("LIMIT_ROTATION")
            limit_rotation.use_limit_y = True
            limit_rotation.min_y = radians(-180) if is_left else 0
            limit_rotation.max_y = 0 if is_left else radians(180)
            limit_rotation.owner_space = "LOCAL"

        if outer_roll_bone := pose_bones.get(f"ik_foot_roll_outer_{suffix}"):
            copy_rotation = outer_roll_bone.constraints.new("COPY_ROTATION")
            copy_rotation.target = target_skeleton
            copy_rotation.subtarget = ctrl_bone_name
            copy_rotation.use_x = False
            copy_rotation.use_y = True
            copy_rotation.use_z = False
            copy_rotation.target_space = "LOCAL"
            copy_rotation.owner_space = "LOCAL"

            limit_rotation = outer_roll_bone.constraints.new("LIMIT_ROTATION")
            limit_rotation.use_limit_y = True
            limit_rotation.min_y = 0 if is_left else radians(-180)
            limit_rotation.max_y = radians(180) if is_left else 0
            limit_rotation.owner_space = "LOCAL"

        if front_roll_bone := pose_bones.get(f"ik_foot_roll_front_{suffix}"):
            copy_rotation = front_roll_bone.constraints.new("COPY_ROTATION")
            copy_rotation.target = target_skeleton
            copy_rotation.subtarget = ctrl_bone_name
            copy_rotation.use_x = False
            copy_rotation.use_y = False
            copy_rotation.use_z = True
            copy_rotation.invert_z = True
            copy_rotation.target_space = "LOCAL"
            copy_rotation.owner_space = "LOCAL"

            limit_rotation = front_roll_bone.constraints.new("LIMIT_ROTATION")
            limit_rotation.use_limit_z = True
            limit_rotation.min_z = 0
            limit_rotation.max_z = radians(180)
            limit_rotation.owner_space = "LOCAL"

        if back_roll_bone := pose_bones.get(f"ik_foot_roll_back_{suffix}"):
            copy_rotation = back_roll_bone.constraints.new("COPY_ROTATION")
            copy_rotation.target = target_skeleton
            copy_rotation.subtarget = ctrl_bone_name
            copy_rotation.use_x = False
            copy_rotation.use_y = False
            copy_rotation.use_z = True
            copy_rotation.invert_z = True
            copy_rotation.target_space = "LOCAL"
            copy_rotation.owner_space = "LOCAL"

            limit_rotation = back_roll_bone.constraints.new("LIMIT_ROTATION")
            limit_rotation.use_limit_z = True
            limit_rotation.min_z = radians(-180)
            limit_rotation.max_z = 0
            limit_rotation.owner_space = "LOCAL"

        if ball_bone := pose_bones.get(f"ball_{suffix}"):
            copy_rotation = ball_bone.constraints.new("COPY_ROTATION")
            copy_rotation.target = target_skeleton
            copy_rotation.subtarget = ctrl_bone_name    
            copy_rotation.use_x = False
            copy_rotation.use_y = False
            copy_rotation.use_z = True
            copy_rotation.invert_z = True
            copy_rotation.mix_mode = "ADD"
            copy_rotation.target_space = "LOCAL"
            copy_rotation.owner_space = "LOCAL"

            limit_rotation = ball_bone.constraints.new("LIMIT_ROTATION")
            limit_rotation.use_limit_z = True
            limit_rotation.min_z = 0
            limit_rotation.max_z = radians(180)
            limit_rotation.owner_space = "LOCAL"

    add_foot_ik_constraints("r")
    add_foot_ik_constraints("l")
    
    # bone name, target name, chain count, use rotation, lock axes
    ik_bones = [
        IKBone("lowerarm_r", "ik_hand_r", "ik_hand_pole_r", 2),
        IKBone("lowerarm_l", "ik_hand_l", "ik_hand_pole_l", 2),
        IKBone("calf_r", "ik_foot_target_r", "ik_foot_pole_r", 2, lock_axes=["X"]),
        IKBone("calf_l", "ik_foot_target_l", "ik_foot_pole_l", 2, lock_axes=["X"]),
        
        IKBone("thumb_03_r", "ik_finger_thumb_r", "ik_finger_pole_thumb_r", 2, pole_angle=0),
        IKBone("index_03_r", "ik_finger_index_r", "ik_finger_pole_index_r", 3, pole_angle=0),
        IKBone("middle_03_r", "ik_finger_middle_r", "ik_finger_pole_middle_r", 3, pole_angle=0),
        IKBone("ring_03_r", "ik_finger_ring_r", "ik_finger_pole_ring_r", 3, pole_angle=0),
        IKBone("pinky_03_r", "ik_finger_pinky_r", "ik_finger_pole_pinky_r", 3, pole_angle=0),
        
        IKBone("thumb_03_l", "ik_finger_thumb_l", "ik_finger_pole_thumb_l", 2, pole_angle=0),
        IKBone("index_03_l", "ik_finger_index_l", "ik_finger_pole_index_l", 3, pole_angle=0),
        IKBone("middle_03_l", "ik_finger_middle_l", "ik_finger_pole_middle_l", 3, pole_angle=0),
        IKBone("ring_03_l", "ik_finger_ring_l", "ik_finger_pole_ring_l", 3, pole_angle=0),
        IKBone("pinky_03_l", "ik_finger_pinky_l", "ik_finger_pole_pinky_l", 3, pole_angle=0),
    ]

    for ik_bone in ik_bones:
        if not (bone := pose_bones.get(ik_bone.bone_name)): continue

        constraint = bone.constraints.new("IK")
        constraint.target = target_skeleton
        constraint.subtarget = ik_bone.target_name
        constraint.chain_count = ik_bone.chain_length
        constraint.use_rotation = ik_bone.use_rotation

        if ik_bone.pole_name:
            constraint.pole_target = target_skeleton
            constraint.pole_subtarget = ik_bone.pole_name
            constraint.pole_angle = radians(ik_bone.pole_angle)
            
        for axis in ik_bone.lock_axes:
            setattr(bone, f"lock_ik_{axis.lower()}", True)

    # bone name, target name, mix, space, weight
    copy_rotation_bones = [
        ("hand_r", "ik_hand_target_r", "POSE", "REPLACE", 1.0),
        ("hand_l", "ik_hand_target_l", "POSE", "REPLACE", 1.0),
        ("foot_r", "ik_foot_rot_ctrl_r", "POSE", "REPLACE", 1.0),
        ("foot_l", "ik_foot_rot_ctrl_l", "POSE", "REPLACE", 1.0),
    ]

    for bone_name, target_name, space, mix, weight in copy_rotation_bones:
        if not (bone := pose_bones.get(bone_name)): continue

        constraint = bone.constraints.new("COPY_ROTATION")
        constraint.target = target_skeleton
        constraint.subtarget = target_name
        constraint.influence = weight
        constraint.target_space = space
        constraint.owner_space = space
        constraint.mix_mode = mix

    # bone name, space, axis -> dict[axis, list[min, max]]
    limit_rotation_bones = [
        ("ik_foot_ctrl_r", "LOCAL", {"X": [0, 0]}),
        ("ik_foot_ctrl_l", "LOCAL", {"X": [0, 0]}),
    ]

    for bone_name, space, axes in limit_rotation_bones:
        if not (bone := pose_bones.get(bone_name)): continue

        constraint = bone.constraints.new("LIMIT_ROTATION")
        constraint.owner_space = space
        
        for axis, min_max in axes.items():
            min = min_max[0]
            max = min_max[1]
            axis_lower = axis.lower()
            
            setattr(constraint, f"use_limit_{axis_lower}", True)
            setattr(constraint, f"min_{axis_lower}", min)
            setattr(constraint, f"max_{axis_lower}", max)

    # bone name, target name, space, weight
    copy_location_bones = [
        ("ik_hand_parent_r", "ik_hand_r", "POSE", 1.0),
        ("ik_hand_parent_l", "ik_hand_l", "POSE", 1.0),
        ("ik_foot_rot_ctrl_r", "ik_foot_r", "POSE", 1.0),
        ("ik_foot_rot_ctrl_l", "ik_foot_l", "POSE", 1.0),
    ]

    for bone_name, target_name, space, weight in copy_location_bones:
        if not (bone := pose_bones.get(bone_name)): continue

        constraint = bone.constraints.new("COPY_LOCATION")
        constraint.target = target_skeleton
        constraint.subtarget = target_name
        constraint.influence = weight
        constraint.target_space = space
        constraint.owner_space = space

    # bone name, object name, palette name, wire width
    
    rig_bones = [
        CustomShape("root", "CTRL_Root", "THEME03"),
        CustomShape("ik_hand_r", "CTRL_Box", "THEME01"),
        CustomShape("ik_hand_l", "CTRL_Box", "THEME04"),
        CustomShape("ik_hand_target_r", "CTRL_Box", "THEME01", scale=0.5),
        CustomShape("ik_hand_target_l", "CTRL_Box", "THEME04", scale=0.5),
        CustomShape("ik_foot_r", "CTRL_Box", "THEME01"),
        CustomShape("ik_foot_l", "CTRL_Box", "THEME04"),
        CustomShape("pelvis", "CTRL_Spine", "THEME12", scale=2.0),
        CustomShape("spine_01", "CTRL_Spine", "THEME12", scale=1.3),
        CustomShape("spine_02", "CTRL_Spine", "THEME12", scale=1.2),
        CustomShape("spine_03", "CTRL_Spine", "THEME12", scale=1.2),
        CustomShape("spine_04", "CTRL_Spine", "THEME12", scale=1.4),
        CustomShape("spine_05", "CTRL_Spine", "THEME12", scale=1.5),
        CustomShape("neck_01", "CTRL_Spine", "THEME12", scale=0.9),
        CustomShape("neck_02", "CTRL_Spine", "THEME12", scale=0.75),
        CustomShape("head", "CTRL_Spine", "THEME12", scale=0.75, offset=(0.025, 0.025, 0.0)),
        CustomShape("clavicle_r", "CTRL_Clavicle", "THEME01"),
        CustomShape("clavicle_l", "CTRL_Clavicle", "THEME04"),
        CustomShape("ball_r", "CTRL_Toe", "THEME01"),
        CustomShape("ball_l", "CTRL_Toe", "THEME04"),
        CustomShape("ik_foot_pole_r", "CTRL_Pole", "THEME01"),
        CustomShape("ik_foot_pole_l", "CTRL_Pole", "THEME04"),
        CustomShape("ik_hand_pole_r", "CTRL_Pole", "THEME01"),
        CustomShape("ik_hand_pole_l", "CTRL_Pole", "THEME04"),
        CustomShape("ik_foot_ctrl_r", "CTRL_Modify", "THEME01"),
        CustomShape("ik_foot_ctrl_l", "CTRL_Modify", "THEME04"),
        
        CustomShape("ik_finger_thumb_r", "CTRL_Box", "THEME01", scale=0.2),
        CustomShape("ik_finger_index_r", "CTRL_Box", "THEME01", scale=0.2),
        CustomShape("ik_finger_middle_r", "CTRL_Box", "THEME01", scale=0.2),
        CustomShape("ik_finger_ring_r", "CTRL_Box", "THEME01", scale=0.2),
        CustomShape("ik_finger_pinky_r", "CTRL_Box", "THEME01", scale=0.2),
        CustomShape("ik_finger_pole_thumb_r", "CTRL_Pole", "THEME01", scale=0.25, rotation=(0, 0, radians(90))),
        CustomShape("ik_finger_pole_index_r", "CTRL_Pole", "THEME01", scale=0.25, rotation=(0, 0, radians(90))),
        CustomShape("ik_finger_pole_middle_r", "CTRL_Pole", "THEME01", scale=0.25, rotation=(0, 0, radians(90))),
        CustomShape("ik_finger_pole_ring_r", "CTRL_Pole", "THEME01", scale=0.25, rotation=(0, 0, radians(90))),
        CustomShape("ik_finger_pole_pinky_r", "CTRL_Pole", "THEME01", scale=0.25, rotation=(0, 0, radians(90))),
        CustomShape("thumb_01_r", "CTRL_Metacarpal", "THEME01", scale_to_bone_length=True),
        CustomShape("index_metacarpal_r", "CTRL_Metacarpal", "THEME01", scale_to_bone_length=True),
        CustomShape("middle_metacarpal_r", "CTRL_Metacarpal", "THEME01", scale_to_bone_length=True),
        CustomShape("ring_metacarpal_r", "CTRL_Metacarpal", "THEME01", scale_to_bone_length=True),
        CustomShape("pinky_metacarpal_r", "CTRL_Metacarpal", "THEME01", scale_to_bone_length=True),

        CustomShape("ik_finger_thumb_l", "CTRL_Box", "THEME04", scale=0.2),
        CustomShape("ik_finger_index_l", "CTRL_Box", "THEME04", scale=0.2),
        CustomShape("ik_finger_middle_l", "CTRL_Box", "THEME04", scale=0.2),
        CustomShape("ik_finger_ring_l", "CTRL_Box", "THEME04", scale=0.2),
        CustomShape("ik_finger_pinky_l", "CTRL_Box", "THEME04", scale=0.2),
        CustomShape("ik_finger_pole_thumb_l", "CTRL_Pole", "THEME04", scale=0.25, rotation=(0, 0, radians(90))),
        CustomShape("ik_finger_pole_index_l", "CTRL_Pole", "THEME04", scale=0.25, rotation=(0, 0, radians(90))),
        CustomShape("ik_finger_pole_middle_l", "CTRL_Pole", "THEME04", scale=0.25, rotation=(0, 0, radians(90))),
        CustomShape("ik_finger_pole_ring_l", "CTRL_Pole", "THEME04", scale=0.25, rotation=(0, 0, radians(90))),
        CustomShape("ik_finger_pole_pinky_l", "CTRL_Pole", "THEME04", scale=0.25, rotation=(0, 0, radians(90))),
        CustomShape("thumb_01_l", "CTRL_Metacarpal", "THEME04", scale_to_bone_length=True),
        CustomShape("index_metacarpal_l", "CTRL_Metacarpal", "THEME04", scale_to_bone_length=True),
        CustomShape("middle_metacarpal_l", "CTRL_Metacarpal", "THEME04", scale_to_bone_length=True),
        CustomShape("ring_metacarpal_l", "CTRL_Metacarpal", "THEME04", scale_to_bone_length=True),
        CustomShape("pinky_metacarpal_l", "CTRL_Metacarpal", "THEME04", scale_to_bone_length=True),
    ]
    
    for custom_shape in rig_bones:
        if not (pose_bone := pose_bones.get(custom_shape.bone_name)): continue
            
        pose_bone.custom_shape = bpy.data.objects.get(custom_shape.object_name)
        pose_bone.color.palette = custom_shape.palette_name
        pose_bone.use_custom_shape_bone_size = custom_shape.scale_to_bone_length
        pose_bone.custom_shape_wire_width = custom_shape.wire_width
        pose_bone.custom_shape_scale_xyz = (custom_shape.scale, custom_shape.scale, custom_shape.scale)
        pose_bone.custom_shape_translation = custom_shape.offset
        pose_bone.custom_shape_rotation_euler = custom_shape.rotation
        rig_collection.assign(pose_bone)
        
    base_bones = [
        "upperarm_r",
        "lowerarm_r",
        "hand_r",
        
        "upperarm_l",
        "lowerarm_l",
        "hand_l",
        
        "thigh_r",
        "calf_r",
        "foot_r",
        
        "thigh_l",
        "calf_l",
        "foot_l",
        
        "thumb_01_r",
        "thumb_02_r",
        "thumb_03_r",

        "index_metacarpal_r",
        "index_01_r",
        "index_02_r",
        "index_03_r",

        "middle_metacarpal_r",
        "middle_01_r",
        "middle_02_r",
        "middle_03_r",
        
        "ring_metacarpal_r",
        "ring_01_r",
        "ring_02_r",
        "ring_03_r",
        
        "pinky_metacarpal_r",
        "pinky_01_r",
        "pinky_02_r",
        "pinky_03_r",

        "thumb_01_l",
        "thumb_02_l",
        "thumb_03_l",

        "index_metacarpal_l",
        "index_01_l",
        "index_02_l",
        "index_03_l",

        "middle_metacarpal_l",
        "middle_01_l",
        "middle_02_l",
        "middle_03_l",

        "ring_metacarpal_l",
        "ring_01_l",
        "ring_02_l",
        "ring_03_l",

        "pinky_metacarpal_l",
        "pinky_01_l",
        "pinky_02_l",
        "pinky_03_l",
    ]
    
    for base_bone in base_bones:
        if not (pose_bone := pose_bones.get(base_bone)): continue
        if len(pose_bone.bone.collections) > 0: continue
        
        base_collection.assign(pose_bone)
        pose_bone.color.palette = "THEME10"
        

    for pose_bone in pose_bones:
        existing_collections = pose_bone.bone.collections
        if any(existing_collections, lambda col: col.name == "Sockets"):
            pose_bone.custom_shape = bpy.data.objects.get("CTRL_Deform")
            pose_bone.custom_shape_scale_xyz = (0.25, 0.25, 0.25)
        
        if len(existing_collections) > 0:
            continue
            
        has_vertex_group = pose_bone.color.palette != "THEME14"
            
        if "dyn_" in pose_bone.name and "_mstr" not in pose_bone.name:
            dynamic_collection.assign(pose_bone)

            pose_bone.custom_shape = bpy.data.objects.get("CTRL_Dynamic")
            pose_bone.color.palette = "THEME07"
            pose_bone.custom_shape_translation = (0.0, 0.025, 0.0)
            
            continue

        if "deform_" in pose_bone.name and has_vertex_group:
            deform_collection.assign(pose_bone)

            pose_bone.custom_shape = bpy.data.objects.get("CTRL_Deform")
            pose_bone.color.palette = "THEME07"

            continue

        if "twist_" in pose_bone.name and has_vertex_group:
            twist_collection.assign(pose_bone)

            pose_bone.custom_shape = bpy.data.objects.get("CTRL_Twist")
            pose_bone.color.palette = "THEME01" if pose_bone.name.endswith("_r") else "THEME04"
            pose_bone.use_custom_shape_bone_size = False
            pose_bone.custom_shape_wire_width = 2.5
            
            continue

        extra_collection.assign(pose_bone)

    bones = target_skeleton.data.bones

    hide_bones = [
        "ik_foot_roll_inner_r",
        "ik_foot_roll_outer_r",
        "ik_foot_roll_front_r",
        "ik_foot_roll_back_r",
        "ik_foot_target_r",
        "ik_foot_rot_ctrl_r",
        "ik_dog_ball_r",
        "ik_wolf_ball_r",
        
        "ik_foot_roll_inner_l",
        "ik_foot_roll_outer_l",
        "ik_foot_roll_front_l",
        "ik_foot_roll_back_l",
        "ik_foot_target_l",
        "ik_foot_rot_ctrl_l",
        "ik_dog_ball_l",
        "ik_wolf_ball_l",

        "ik_hand_parent_r",
        "ik_hand_parent_l",
    ]

    for bone_name in hide_bones:
        if not (bone := bones.get(bone_name)): continue
        bone.hide = True
            

    bpy.ops.object.mode_set(mode='OBJECT')
        
    