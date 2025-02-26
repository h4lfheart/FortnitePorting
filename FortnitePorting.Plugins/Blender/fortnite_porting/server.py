from threading import Thread, Event
from werkzeug.serving import make_server
from flask import Flask, request
from .logger import Log
from collections import deque

class Server(Thread):
    def __init__(self, app):
        Thread.__init__(self, daemon=True)
        self.server = make_server('127.0.0.1', 20000, app)
        self.context = app.app_context()
        self.context.push()

        self.queue = deque()

    @staticmethod
    def create():
        app = Flask("Fortnite Porting Server")
        server = Server(app)

        @app.route('/fortnite-porting/data', methods=['POST'])
        def post_data():
            data = request.get_data().decode('utf-8')
            server.queue.append(data)
            return data

        @app.route('/fortnite-porting/ping', methods=['GET'])
        def ping():
            return "Pong!"

        return server

    def run(self):
        Log.info("Started Fortnite Porting Server")
        self.server.serve_forever()

    def shutdown(self):
        Log.info("Shutdown Fortnite Porting Server")
        self.server.shutdown()

    def get_data(self):
        if len(self.queue) > 0:
            return self.queue.popleft()
        else:
            return None
