import json
import socket
from .logger import Log
from threading import Thread, Event

HOST = "127.0.0.1"
IMPORT_PORT = 24000
MESSAGE_PORT = 24001
BUFFER_SIZE = 1024

def decode_bytes(data, format = "utf-8", verbose = False):
	try:
		return data.decode(format)
	except UnicodeDecodeError as e:
		return None

def encode_string(string, format = "utf-8"):
	return string.encode(format)

class ServerBase(Thread):
	def __init__(self, host, port, is_server=False):
		Thread.__init__(self, daemon=True)
		self.socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
		self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
		if not is_server:
			self.socket.bind((host, port))


class ImportServer(ServerBase):
	def __init__(self):
		super().__init__(HOST, IMPORT_PORT)
		self.event = Event()

	def run(self):
		self.processing = True
		Log.info(f"FortnitePorting Server Started at {HOST}:{IMPORT_PORT}")

		while self.processing:
			try:
				self.receive()
			except OSError as e:
				pass

	def stop(self):
		self.socket.close()
		self.processing = False
		Log.info(f"FortnitePorting Server Stopped at {HOST}:{IMPORT_PORT}")

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

	def ping(self, sender):
		self.socket.sendto(encode_string("Pong"), sender)

	def has_response(self):
		return self.event.is_set()

	def clear_response(self):
		self.event.clear()
		
class MessageServer(ServerBase):
	instance = None
	
	def __init__(self):
		super().__init__(HOST, MESSAGE_PORT, is_server=True)
		MessageServer.instance = self

	def run(self):
		Log.info(f"FortnitePorting Message Client Started at {HOST}:{MESSAGE_PORT}")

	def stop(self):
		self.socket.close()
		Log.info(f"FortnitePorting Message Client Stopped at {HOST}:{MESSAGE_PORT}")
		
	def send(self, data):
		self.socket.sendto(encode_string(data), (HOST, MESSAGE_PORT))
