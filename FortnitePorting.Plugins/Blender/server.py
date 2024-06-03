from threading import Thread, Event
from werkzeug.serving import make_server
from flask import Flask, request
from .logger import Log


class Server(Thread):
    def __init__(self, app):
        Thread.__init__(self, daemon=True)
        self.server = make_server('127.0.0.1', 20000, app)
        self.context = app.app_context()
        self.context.push()

        self.event = Event()
        self.data = ""

    @staticmethod
    def create():
        app = Flask("Fortnite Porting Server")
        server = Server(app)

        @app.route('/fortnite-porting/data', methods=['POST'])
        def post_data():
            server.data = request.get_data().decode('utf-8')
            server.event.set()
            return server.data

        return server

    def run(self):
        Log.info("Started Fortnite Porting Server")
        self.server.serve_forever()

    def shutdown(self):
        Log.info("Shutdown Fortnite Porting Server")
        self.server.shutdown()

    def get_data(self):
        if self.event.is_set():
            self.event.clear()
            return self.data
        else:
            return None
