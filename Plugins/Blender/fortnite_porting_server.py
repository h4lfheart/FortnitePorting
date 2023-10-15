import bpy
import json
import socket
import traceback
import os
import ue_format as ueformat
from enum import Enum
from threading import Thread, Event

bl_info = {
	"name": "Fortnite Porting",
	"author": "Half",
	"version": (2, 0, 0),
	"blender": (4, 0, 0),
	"description": "Fortnite Porting Blender Plugin",
	"category": "Import",
}

HOST = "127.0.0.1"
PORT = 24000
BUFFER_SIZE = 1024

class MeshType(Enum):
	uemodel = 0
	psk = 1

def decode_bytes(data, format = "utf-8", verbose = False):
	try:
		return data.decode(format)
	except UnicodeDecodeError as e:
		return None

def encode_string(string, format = "utf-8"):
	return string.encode(format)

class Log:
	INFO = u"\u001b[36m"
	WARNING = u"\u001b[31m"
	ERROR = u"\u001b[33m"
	RESET = u"\u001b[0m"

	@staticmethod
	def info(message):
		print(f"{Log.INFO}[INFO] {Log.RESET}{message}")

	@staticmethod
	def warn(message):
		print(f"{Log.WARNING}[WARN] {Log.RESET}{message}")
		
	@staticmethod
	def error(message):
		print(f"{Log.WARNING}[ERROR] {Log.RESET}{message}")

class Server(Thread):
	def __init__(self):
		Thread.__init__(self, daemon=True)
		self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
		self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
		self.event = Event()

	def run(self):
		self.socket.bind((HOST, PORT))
		self.processing = True
		Log.info(f"FortnitePorting Server Started at {HOST}:{PORT}")

		while self.processing:
			try:
				self.receive()
			except OSError as e:
				pass

	def stop(self):
		self.socket.close()
		self.processing = False
		Log.info(f"FortnitePorting Server Stopped at {HOST}:{PORT}")

	def receive(self):
		full_data = ""
		while True:
			byte_data, sender = self.socket.recvfrom(BUFFER_SIZE)
			if string_data := decode_bytes(byte_data):
				match string_data:
					case "Start":
						pass
					case "Stop":
						break
					case "Ping":
						self.ping(sender)
					case _:
						full_data += string_data
		self.response = json.loads(full_data)
		self.event.set()
		self.ping(sender)

	def ping(self, sender):
		self.socket.sendto(encode_string("Pong"), sender)

	def has_response(self):
		return self.event.is_set()

	def clear_response(self):
		self.event.clear()

def import_mesh(path: str) -> bpy.types.Object:
    path = path[1:] if path.startswith("/") else path
    mesh_path = os.path.join(assets_folder, path.split(".")[0])

    extension = MeshType(options.get("MeshExportType")).name
    if os.path.exists(f"{mesh_path}.{extension}"):
        mesh_path += f".{extension}"

    return ueformat.import_file(mesh_path)

def import_response(response):
	print(response)

	global assets_folder
	assets_folder = response.get("AssetsFolder")

	global options
	options = response.get("Options")

	data = response.get("Data")
	for mesh in data.get("Meshes"):
		import_mesh(mesh.get("Path"))

def message_box(message = "", title = "Message Box", icon = 'INFO'):

	def draw(self, context):
		self.layout.label(text=message)

	bpy.context.window_manager.popup_menu(draw, title = title, icon = icon)

def register():
	ueformat.register()

	global server
	server = Server()
	server.start()

	def import_handler():
		if server.has_response():
			try:
				import_response(server.response)
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