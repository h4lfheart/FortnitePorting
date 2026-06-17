import bpy
import traceback
from .server import Server
from .logger import Log
from .processing.importer import Importer
from .operator.tasty_op import TASTY_PT_RigSettings
from .operator import drag_drop_op

from .ueformat import register as ueformat_register, unregister as ueformat_unregister

from .processing.context.material_context import material_hash_cache, material_name_cache
from .processing.context.texture_context import image_cache
from .utils import loaded_versions

from bpy.app.handlers import persistent


bl_info = {
    "name": "Fortnite Porting",
    "description": "Import Server for Fortnite Porting",
    "author": "Half",
    "blender": (5, 0, 0),
    "version": (4, 1, 6),
    "category": "Import-Export",
}


def display_popup(text="Message", title="Information", icon='INFO'):
    def draw(self, context):
        self.layout.label(text=text)

    bpy.context.window_manager.popup_menu(draw, title=title, icon=icon)


def server_data_handler():
    if data := server.get_data():
        try:
            Importer.Import(data)
        except Exception as e:
            error_message = str(e)

            Log.error(f"An unhandled error occurred:")
            traceback.print_exc()

            display_popup(error_message, "An unhandled error occurred", "ERROR")

    return 0.01

@persistent
def scene_load_handler(filepath):
    material_hash_cache.clear()
    material_name_cache.clear()
    image_cache.clear()
    loaded_versions.clear()


def register():
    global server
    server = Server.create()
    server.start()

    bpy.app.timers.register(server_data_handler, persistent=True)
    bpy.app.handlers.load_post.append(scene_load_handler)

    bpy.utils.register_class(TASTY_PT_RigSettings)
    drag_drop_op.register()
    ueformat_register()


def unregister():
    server.shutdown()

    bpy.app.handlers.load_post.remove(scene_load_handler)

    bpy.utils.unregister_class(TASTY_PT_RigSettings)
    drag_drop_op.unregister()
    ueformat_unregister()