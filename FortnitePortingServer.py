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
                dataString = ""
                while True:
                    info = self.socket_server.recvfrom(4096)
                    if data := info[0].decode('utf-8'):
                        if data == "FPMessageFinished":
                            break
                        dataString += data
                self.event.set()
                self.data = json.loads(dataString)
            except OSError:
                pass

    def stop(self):
        self.keep_alive = False
        self.socket_server.close()
        Log.information("FortnitePorting Server Closed")


class Utils:
    material_mappings = {
        ("Diffuse", 0, (-300, -75)),
        ("SpecularMasks", 1, (-300, -125), True),
        ("Normals", 2, (-300, -175), True),
        ("Normal", 2, (-300, -175), True)
    }

    @staticmethod
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

    @staticmethod
    def import_texture(path: str) -> bpy.types.Image:
        path, name = path.split(".")
        if existing := bpy.data.images.get(name):
            return existing

        path = path[1:] if path.startswith("/") else path
        texture_path = os.path.join(import_assets_root, path + ".png")

        if not os.path.exists(texture_path):
            return None

        return bpy.data.images.load(texture_path, check_existing=True)

    @staticmethod
    def import_material(target_slot: bpy.types.MaterialSlot, material_data):
        material_name = material_data.get("MaterialName")
        if (existing := bpy.data.materials.get(material_name)) and existing.use_nodes is True:  # assume default psk mat
            target_slot.material = existing
            return
        target_material = target_slot.material
        if target_material.name != material_name:
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
            path = data.get("Path")

            if (info := Utils.First(Utils.material_mappings, lambda x: x[0] == name)) is None:
                return

            _, slot, location, *linear = info

            if (image := Utils.import_texture(path)) is None:
                return

            node = nodes.new(type="ShaderNodeTexImage")
            node.image = image
            node.image.alpha_mode = 'CHANNEL_PACKED'
            node.hide = True
            node.location = location

            if linear:
                node.image.colorspace_settings.name = "Linear"

            links.new(node.outputs[0], shader_node.inputs[slot])

        for texture in material_data.get("Textures"):
            texture_parameter(texture)

    @staticmethod
    def mesh_from_armature(armature) -> bpy.types.Mesh:
        return armature.children[0]  # only used with psk, mesh is always first child

    @staticmethod
    def First(target, expr, default=None):
        if not target:
            return None
        Filtered = filter(expr, target)

        return next(Filtered, default)


def import_response(response):
    global import_assets_root
    import_assets_root = response.get("AssetsRoot")

    global import_settings
    import_settings = response.get("Settings")

    global import_data
    import_data = response.get("Data")

    name = import_data.get("Name")
    type = import_data.get("Type")

    Log.information(f"Received Import for {type}: {name}")
    print(import_data)

    imported_parts = {}
    for part in import_data.get("Parts"):
        if (armature := Utils.import_mesh(part.get("MeshPath"))) is None:
            continue
        mesh = Utils.mesh_from_armature(armature)
        bpy.context.view_layer.objects.active = mesh

        for material in part.get("Materials"):
            index = material.get("SlotIndex")
            Utils.import_material(mesh.material_slots.values()[index], material)

        for override_material in part.get("OverrideMaterials"):
            index = override_material.get("SlotIndex")
            Utils.import_material(mesh.material_slots.values()[index], override_material)


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
