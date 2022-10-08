import bpy, threading, json, socket
from io_import_scene_unreal_psa_psk_280 import pskimport, psaimport

bl_info = {
    "name": "Fortnite Porting",
    "author": "Half",
    "version": (1, 0, 0),
    "blender": (3, 0, 0),
    "description": "Blender Server for Fortnite Porting",
    "category": "Import",
}

class Receiver(threading.Thread):

    def __init__(self, event, data):
        threading.Thread.__init__(self, daemon=True)
        self.event = event
        self.data = data
        self.socketServer = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.shouldEndThread = False

    def run(self):
        host, port = 'localhost', 24280
        self.socketServer.bind((host, port))
        print(f"[INFO] FortnitePorting Server Listening at {host}:{port}")

        while True:
            if self.shouldEndThread:
                return
            buffer_size = 4096*2
            try:
                bap = self.socketServer.recvfrom(buffer_size)
                data = bap[0]
                if self.data:
                    jsonData = json.loads(data.decode('utf-8'))
                    self.data[0] = jsonData
                    self.event.set()
            except OSError:
                pass
                
    def stop(self):
        self.socketServer.close()
        print(f"[INFO] FortnitePorting Server Closed")
        self.shouldEndThread = True


def register():
    data = [None]
    event = threading.Event()
    
    global server
    server = Receiver(event, data)
    server.start()

    def handler():
        sent = data[0]
        if event.is_set():
            items = sent.get("Data").get("Meshes")
            for item in items:
                pskimport(item)
            event.clear()
        return 0.1

    bpy.app.timers.register(handler)
    
def unregister():
    server.stop()
