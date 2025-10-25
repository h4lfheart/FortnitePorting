from threading import Thread
from http.server import HTTPServer, BaseHTTPRequestHandler
from urllib.parse import urlparse
import json
from collections import deque
from .logger import Log


class RequestHandler(BaseHTTPRequestHandler):
    def __init__(self, server_instance, *args, **kwargs):
        self.server_instance = server_instance
        super().__init__(*args, **kwargs)

    def do_POST(self):
        parsed_path = urlparse(self.path)

        if parsed_path.path == '/fortnite-porting/data':
            try:
                content_length = int(self.headers.get('Content-Length', 0))
                data = self.rfile.read(content_length).decode('utf-8')

                self.server_instance.queue.append(data)

                self.send_response(200)
                self.send_header('Content-Type', 'text/plain')
                self.end_headers()
                self.wfile.write(data.encode('utf-8'))

            except Exception as e:
                self.send_response(500)
                self.send_header('Content-Type', 'text/plain')
                self.end_headers()
                self.wfile.write(f"Error: {str(e)}".encode('utf-8'))
        else:
            self.send_response(404)
            self.end_headers()

    def do_GET(self):
        parsed_path = urlparse(self.path)

        if parsed_path.path == '/fortnite-porting/ping':
            self.send_response(200)
            self.send_header('Content-Type', 'text/plain')
            self.end_headers()
            self.wfile.write(b"Pong!")
        else:
            self.send_response(404)
            self.end_headers()

    def log_message(self, format, *args):
        # Suppress default logging
        pass


class Server(Thread):
    def __init__(self):
        Thread.__init__(self, daemon=True)
        self.queue = deque()

        # Create handler class with reference to this server instance
        def handler_factory(*args, **kwargs):
            return RequestHandler(self, *args, **kwargs)

        self.server = HTTPServer(('127.0.0.1', 20000), handler_factory)

    @staticmethod
    def create():
        return Server()

    def run(self):
        Log.info("Running Server at http://127.0.0.1/fortnite-porting/")
        try:
            self.server.serve_forever()
        except Exception as e:
            Log.error(f"Server error: {e}")

    def shutdown(self):
        Log.info("Shutdown Server")
        self.server.shutdown()
        self.server.server_close()

    def get_data(self):
        if len(self.queue) > 0:
            return self.queue.popleft()
        else:
            return None