import bpy
import json
import os
import socket
import threading
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
    RESET = u"\u001b[0m"

    @staticmethod
    def information(message):
        print(f"{Log.INFO}[INFO] {Log.RESET}{message}")

    @staticmethod
    def warning(message):
        print(f"{Log.WARNING}[WARN] {Log.RESET}{message}")


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

    if not pskimport(mesh_path):
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




def mesh_from_armature(armature) -> bpy.types.Mesh:
    return armature.children[0]  # only used with psk, mesh is always first child


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
    global import_assets_root
    import_assets_root = response.get("AssetsRoot")

    global import_settings
    import_settings = response.get("Settings")

    global import_data
    import_data = response.get("Data")

    name = import_data.get("Name")
    import_type = import_data.get("Type")

    Log.information(f"Received Import for {import_type}: {name}")
    print(import_data)

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
                "Mesh": mesh
            })
            
            if morph_name := part.get("MorphName"):
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
                
    bpy.ops.object.select_all(action='DESELECT')
    





def register():
    import_event = threading.Event()

    global server
    server = Receiver(import_event)
    server.start()

    def handler():
        if import_event.is_set():
            import_response(server.data)
            import_event.clear()
        return 0.01

    bpy.app.timers.register(handler)


def unregister():
    server.stop()
