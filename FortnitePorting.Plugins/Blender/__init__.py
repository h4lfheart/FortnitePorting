import bpy
from .server import Server
from .logger import Log


def server_data_handler():
    if data := server.get_data():
        Log.info("Importing " + data)
    return 0.01


def register():
    global server
    server = Server.create()
    server.start()

    bpy.app.timers.register(server_data_handler, persistent=True)


def unregister():
    server.shutdown()
