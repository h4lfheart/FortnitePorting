import bpy
import traceback
from . import ue_format as ueformat
from .logger import Log
from .server import Server
from .import_task import ImportTask

bl_info = {
	"name": "Fortnite Porting",
	"description": "Fortnite Porting Blender Plugin",
	"author": "Half",
	"blender": (4, 0, 0),
	"version": (2, 0, 0),
	"category": "Import",
}

def message_box(message = "", title = "Message Box", icon = 'INFO'):

	def draw(self, context):
		self.layout.label(text=message)

	bpy.context.window_manager.popup_menu(draw, title = title, icon = icon)

def register():
	import importlib
	importlib.reload(ueformat)
	ueformat.register()

	global server
	server = Server()
	server.start()

	def import_handler():
		if server.has_response():
			try:
				ImportTask().run(server.response)
			except Exception as e:
				error_str = str(e)
				Log.error(f"An unhandled error occurred:")
				traceback.print_exc()
				message_box(error_str, "An unhandled error occurred", "ERROR")
			server.clear_response()
		return 0.01

	bpy.app.timers.register(import_handler, persistent=True)

def unregister():
	ueformat.unregister()
	server.stop()

if __name__ == "__main__":
	register()	