import bpy
import json
import os
import socket
import threading
import re
from math import radians
from mathutils import Matrix, Vector
from io_import_scene_unreal_psa_psk_280 import pskimport

bl_info = {
    "name": "Fortnite Porting",
    "author": "Half",
    "version": (1, 0, 0),
    "blender": (3, 0, 0),
    "description": "Blender Server for Fortnite Porting",
    "category": "Import",
}

global import_assets_root
global import_settings
global import_data

global server

class Log:
    INFO = u"\u001b[36m"
    WARNING = u"\u001b[31m"
    ERROR = u"\u001b[33m"
    RESET = u"\u001b[0m"

    @staticmethod
    def information(message):
        print(f"{Log.INFO}[INFO] {Log.RESET}{message}")

    @staticmethod
    def warning(message):
        print(f"{Log.WARNING}[WARN] {Log.RESET}{message}")
        
    @staticmethod
    def error(message):
        print(f"{Log.WARNING}[ERROR] {Log.RESET}{message}")


class Receiver(threading.Thread):

    def __init__(self, event):
        threading.Thread.__init__(self, daemon=True)
        self.event = event
        self.data = None
        self.socket_server = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.keep_alive = True

    def run(self):
        host, port = 'localhost', 24280
        self.socket_server.bind((host, port))
        self.socket_server.settimeout(1.0)
        Log.information(f"FortnitePorting Server Listening at {host}:{port}")

        while self.keep_alive:
            try:
                data_string = ""
                while True:
                    info = self.socket_server.recvfrom(4096)
                    if data := info[0].decode('utf-8'):
                        if data == "FPMessageFinished":
                            break
                        data_string += data
                self.event.set()
                self.data = json.loads(data_string)
            except OSError:
                pass

    def stop(self):
        self.keep_alive = False
        self.socket_server.close()
        Log.information("FortnitePorting Server Closed")


# Name, Slot, Location, *Linear
texture_mappings = {
    ("Diffuse", 0, (-300, -75)),
    ("PetalDetailMap", 0, (-300, -75)),

    ("SpecularMasks", 1, (-300, -125), True),
    ("SpecMap", 1, (-300, -125), True),

    ("Normals", 5, (-300, -175), True),
    ("Normal", 5, (-300, -175), True),
    ("NormalMap", 5, (-300, -175), True),

    ("M", 7, (-300, -225), True),

    ("Emissive", 12, (-300, -325)),
    ("EmissiveTexture", 12, (-300, -325)),
}

# Name, Slot
scalar_mappings = {
    ("RoughnessMin", 3),
    ("Roughness Min", 3),
    ("SpecRoughnessMin", 3),

    ("RoughnessMax", 4),
    ("Roughness Max", 4),
    ("SpecRoughnessMax", 4),

    ("emissive mult", 13),
    ("TH_StaticEmissiveMult", 13),
    ("Emissive", 13),

    ("Emissive_BrightnessMin", 14),

    ("Emissive_BrightnessMax", 15),

    ("Emissive Fres EX", 16),
    ("EmissiveFresnelExp", 16),
}

# Name, Slot, *Alpha
vector_mappings = {
    ("Skin Boost Color And Exponent", 10, 11),

    ("EmissiveColor", 18, 17),
    ("Emissive Color", 18, 17)
}


def import_mesh(path: str) -> bpy.types.Object:
    path = path[1:] if path.startswith("/") else path
    mesh_path = os.path.join(import_assets_root, path.split(".")[0] + "_LOD0")

    if os.path.exists(mesh_path + ".psk"):
        mesh_path += ".psk"
    if os.path.exists(mesh_path + ".pskx"):
        mesh_path += ".pskx"

    if not pskimport(mesh_path, bReorientBones=import_settings.get("ReorientBones")):
        return None

    return bpy.context.active_object


def import_texture(path: str) -> bpy.types.Image:
    path, name = path.split(".")
    if existing := bpy.data.images.get(name):
        return existing

    path = path[1:] if path.startswith("/") else path
    texture_path = os.path.join(import_assets_root, path + ".png")

    if not os.path.exists(texture_path):
        return None

    return bpy.data.images.load(texture_path, check_existing=True)


def import_material(target_slot: bpy.types.MaterialSlot, material_data):
    material_name = material_data.get("MaterialName")
    if (existing := bpy.data.materials.get(material_name)) and existing.use_nodes is True:  # assume default psk mat
        target_slot.material = existing
        return
    target_material = target_slot.material
    if target_material.name.casefold() != material_name.casefold():
        target_material = target_material.copy()
        target_material.name = material_name
        target_slot.material = target_material
    target_material.use_nodes = True

    nodes = target_material.node_tree.nodes
    nodes.clear()
    links = target_material.node_tree.links
    links.clear()

    output_node = nodes.new(type="ShaderNodeOutputMaterial")
    output_node.location = (200, 0)

    shader_node = nodes.new(type="ShaderNodeGroup")
    shader_node.name = "Fortnite Porting"
    shader_node.node_tree = bpy.data.node_groups.get(shader_node.name)

    links.new(shader_node.outputs[0], output_node.inputs[0])

    def texture_parameter(data):
        name = data.get("Name")
        value = data.get("Value")

        if (info := first(texture_mappings, lambda x: x[0].casefold() == name.casefold())) is None:
            return

        _, slot, location, *linear = info

        if slot is 12 and value.endswith("_FX"):
            return

        if (image := import_texture(value)) is None:
            return

        node = nodes.new(type="ShaderNodeTexImage")
        node.image = image
        node.image.alpha_mode = 'CHANNEL_PACKED'
        node.hide = True
        node.location = location

        if linear:
            node.image.colorspace_settings.name = "Linear"

        links.new(node.outputs[0], shader_node.inputs[slot])

    def scalar_parameter(data):
        name = data.get("Name")
        value = data.get("Value")

        if (info := first(scalar_mappings, lambda x: x[0].casefold() == name.casefold())) is None:
            return

        _, slot = info

        shader_node.inputs[slot].default_value = value

    def vector_parameter(data):
        name = data.get("Name")
        value = data.get("Value")

        if (info := first(vector_mappings, lambda x: x[0].casefold() == name.casefold())) is None:
            return

        _, slot, *extra = info

        shader_node.inputs[slot].default_value = (value["R"], value["G"], value["B"], 1)

        if extra[0]:
            try:
                shader_node.inputs[extra[0]].default_value = value["A"]
            except TypeError:
                shader_node.inputs[extra[0]].default_value = int(value["A"])

    for texture in material_data.get("Textures"):
        texture_parameter(texture)

    for scalar in material_data.get("Scalars"):
        scalar_parameter(scalar)

    for vector in material_data.get("Vectors"):
        vector_parameter(vector)

def merge_skeletons(parts) -> bpy.types.Armature:
    bpy.ops.object.select_all(action='DESELECT')

    merge_parts = []
    constraint_parts = []
    
    # Merge Skeletons
    for part in parts:
        slot = part.get("Part")
        skeleton = part.get("Armature")
        mesh = part.get("Mesh")
        socket = part.get("Socket")
        if slot == "Body":
            bpy.context.view_layer.objects.active = skeleton

        if slot not in {"Hat", "MiscOrTail"} or socket == "Face" or (socket == "None" and slot != "MiscOrTail"):
            skeleton.select_set(True)
            merge_parts.append(part)
        else:
            constraint_parts.append(part)

    bpy.ops.object.join()
    master_skeleton = bpy.context.active_object
    bpy.ops.object.select_all(action='DESELECT')

    # Merge Meshes
    for part in merge_parts:
        slot = part.get("Part")
        mesh = part.get("Mesh")
        if slot == "Body":
            bpy.context.view_layer.objects.active = mesh
        mesh.select_set(True)
            
    bpy.ops.object.join()
    bpy.ops.object.select_all(action='DESELECT')

    # Remove Duplicates and Rebuild Bone Tree
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
    
    # Manual Bone Tree Fixes
    other_fixes = {
        "L_eye_lid_lower_mid": "faceAttach",
        "L_eye_lid_upper_mid": "faceAttach",
        "R_eye_lid_lower_mid": "faceAttach",
        "R_eye_lid_upper_mid": "faceAttach",
        "dyn_spine_05": "spine_05"
    }
    
    for bone, parent in other_fixes.items():
        if target_bone := skeleton_bones.get(bone):
            target_bone.parent = skeleton_bones.get(parent)
       
    bpy.ops.object.mode_set(mode='OBJECT')     
    
    # TODO ACTUAL SOCKET BONES
    # Object Constraints w/ Sockets
    for part in constraint_parts:
        slot = part.get("Part")
        skeleton = part.get("Armature")
        mesh = part.get("Mesh")
        socket = part.get("Socket").lower()
        
        if socket == "hat":
            constraint_object(skeleton, master_skeleton, "head")
        if socket == "tail":
            constraint_object(skeleton, master_skeleton, "pelvis")
        if socket == "none" and part == "MiscOrTail":
            constraint = skeleton.pose.bones[0].constraints.new('CHILD_OF')
            constraint.target = master_skeleton
            constraint.subtarget = skeleton.pose.bones[0].name
  
    return master_skeleton

def constraint_object(child: bpy.types.Object, parent: bpy.types.Object, bone: str, rot=[radians(0), radians(90), radians(0)]):
    constraint = child.constraints.new('CHILD_OF')
    constraint.target = parent
    constraint.subtarget = bone
    child.rotation_mode = 'XYZ'
    child.rotation_euler = rot
    constraint.inverse_matrix = Matrix.Identity(4)

def mesh_from_armature(armature) -> bpy.types.Mesh:
    return armature.children[0]  # only used with psk, mesh is always first child

def append_data():
    addon_dir = os.path.dirname(os.path.splitext(__file__)[0])
    with bpy.data.libraries.load(os.path.join(addon_dir, "FortnitePortingData.blend")) as (data_from, data_to):
        if not bpy.data.node_groups.get("Fortnite Porting"):
             data_to.node_groups = data_from.node_groups

def first(target, expr, default=None):
    if not target:
        return None
    filtered = filter(expr, target)

    return next(filtered, default)

def where(target, expr):
    if not target:
        return None
    filtered = filter(expr, target)

    return filtered

def any(target, expr):
    if not target:
        return None

    filtered = list(filter(expr, target))
    return len(filtered) > 0


def import_response(response):
    append_data()
    global import_assets_root
    import_assets_root = response.get("AssetsRoot")

    global import_settings
    import_settings = response.get("Settings")

    global import_data
    import_data = response.get("Data")

    name = import_data.get("Name")
    import_type = import_data.get("Type")

    Log.information(f"Received Import for {import_type}: {name}")
    print(response)

    imported_parts = []
    def import_part(parts):
        for part in parts:
            part_type = part.get("Part")
            if any(imported_parts, lambda x: False if x is None else x.get("Part") == part_type):
                continue

            if (imported_part := import_mesh(part.get("MeshPath"))) is None:
                continue

            has_armature = imported_part.type == "ARMATURE"
            if has_armature:
                mesh = mesh_from_armature(imported_part)
            else:
                mesh = imported_part
            bpy.context.view_layer.objects.active = mesh

            imported_parts.append({
                "Part": part_type,
                "Armature": imported_part if has_armature else None,
                "Mesh": mesh,
                "Socket": part.get("SocketName")
            })
            
            if (morph_name := part.get("MorphName")) and mesh.data.shape_keys is not None:
                for key in mesh.data.shape_keys.key_blocks:
                    if key.name.casefold() == morph_name.casefold():
                        key.value = 1.0

            for material in part.get("Materials"):
                index = material.get("SlotIndex")
                import_material(mesh.material_slots.values()[index], material)

            for override_material in part.get("OverrideMaterials"):
                index = override_material.get("SlotIndex")
                import_material(mesh.material_slots.values()[index], override_material)

    import_part(import_data.get("StyleParts"))
    import_part(import_data.get("Parts"))

    for imported_part in imported_parts:
        mesh = imported_part.get("Mesh")
        for style_material in import_data.get("StyleMaterials"):
            if slot := mesh.material_slots.get(style_material.get("MaterialNameToSwap")):
                import_material(slot, style_material)
                
    if (import_settings.get("MergeSkeletons")):
        merge_skeletons(imported_parts)


    bpy.ops.object.select_all(action='DESELECT')
    
def message_box(message = "", title = "Message Box", icon = 'INFO'):

    def draw(self, context):
        self.layout.label(text=message)

    bpy.context.window_manager.popup_menu(draw, title = title, icon = icon)

def register():
    import_event = threading.Event()

    global server
    server = Receiver(import_event)
    server.start()

    def handler():
        if import_event.is_set():
            try:
               import_response(server.data)
            except Exception as e:
                error_str = str(e)
                Log.error(f"An unhandled error occurred: {error_str}")
                message_box(error_str, "An unhandled error occurred", "ERROR")
                
            import_event.clear()
        return 0.01

    bpy.app.timers.register(handler)


def unregister():
    server.stop()
