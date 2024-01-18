import bpy
import traceback
import zstandard as zstd
from .logger import Log
from .server import ImportServer, MessageServer
from .import_task import ImportTask
from . import ue_format

bl_info = {
    "name": "Fortnite Porting",
    "description": "Fortnite Porting Blender Plugin",
    "author": "Half",
    "blender": (4, 0, 0),
    "version": (2, 0, 3),
    "category": "Import",
}


def message_box(message="", title="Message Box", icon='INFO'):
    def draw(self, context):
        self.layout.label(text=message)

    bpy.context.window_manager.popup_menu(draw, title=title, icon=icon)


def register():
    ue_format.zstd_decompresser = zstd.ZstdDecompressor()

    global import_server
    import_server = ImportServer()
    import_server.start()

    def import_server_handler():
        if import_server.has_response():
            try:
                ImportTask().run(import_server.response)
            except Exception as e:
                error_str = str(e)
                Log.error(f"An unhandled error occurred:")
                traceback.print_exc()
                message_box(error_str, "An unhandled error occurred", "ERROR")
            import_server.clear_response()
        return 0.01

    bpy.app.timers.register(import_server_handler, persistent=True)

    global message_server
    message_server = MessageServer()
    message_server.start()


def unregister():
    import_server.stop()
    message_server.stop()


if __name__ == "__main__":
    register()
