import json
import socket
from .logger import Log
from threading import Thread, Event

HOST = "127.0.0.1"
PORT = 24000
BUFFER_SIZE = 1024

def decode_bytes(data, format = "utf-8", verbose = False):
	try:
		return data.decode(format)
	except UnicodeDecodeError as e:
		return None

def encode_string(string, format = "utf-8"):
	return string.encode(format)

class Server(Thread):
	instance = None

	def __init__(self):
		Thread.__init__(self, daemon=True)
		Server.instance = self
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
			self.most_recent_sender = sender
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
		
	def send(self, command):
		self.socket.sendto(encode_string(command), self.most_recent_sender)

	def ping(self, sender):
		self.socket.sendto(encode_string("Pong"), sender)

	def has_response(self):
		return self.event.is_set()

	def clear_response(self):
		self.event.clear()