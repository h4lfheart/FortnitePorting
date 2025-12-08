import socket
import struct
import threading
import json
from threading import Thread
from collections import deque
from .logger import Log

COMMAND_MESSAGE = 0
COMMAND_DATA = 1

class Server(Thread):
    instance = None
    
    def __init__(self):
        Thread.__init__(self, daemon=True)
        self.queue = deque()
        self.host = '127.0.0.1'
        self.port = 40000
        self.server = None
        self.running = False
        self.clients = []

    @staticmethod
    def create():
        Server.instance = Server()
        return Server.instance

    def run(self):
        Log.info(f"Running FP V4 Server at {self.host}:{self.port}")
        try:
            self.server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.server.bind((self.host, self.port))
            self.server.listen(5)
            self.running = True
            
            while self.running:
                try:
                    client_socket, address = self.server.accept()
                    Log.info(f"Received connection from {address}")
                    
                    self.clients.append(client_socket)
                    
                    client_thread = Thread(
                        target=self.handle_client,
                        args=(client_socket, address)
                    )
                    client_thread.daemon = True
                    client_thread.start()
                    
                except Exception as e:
                    if self.running:
                        Log.error(f"Error accepting connection: {e}")
                    break
                    
        except Exception as e:
            Log.error(f"Server error: {e}")

    def recv_exact(self, sock, n):
        data = b''
        while len(data) < n:
            packet = sock.recv(n - len(data))
            if not packet:
                return None
            data += packet
        return data

    def handle_client(self, client_socket, address):
        try:
            while True:
                header = self.recv_exact(client_socket, 5)
                if not header:
                    break
                
                command_type, data_size = struct.unpack('=BI', header)
                if not (data := self.recv_exact(client_socket, data_size)):
                    break

                decoded_string = data.decode('utf-8')
                
                if command_type == COMMAND_MESSAGE:
                    Log.info(f"Message: {decoded_string}")
                elif command_type == COMMAND_DATA:
                    Log.info(f"Received data with size {round(data_size / (1024 ** 2), 3)}MB")
                    self.queue.append(decoded_string)
                
                        
        except Exception as e:
            Log.error(f"Error handling client {address}: {e}")
        finally:
            client_socket.close()
            Log.info(f"Connection closed: {address}")
            
    
    def send_message(self, message):
        json_str = json.dumps(message)
        data = json_str.encode('utf-8')
        
        header = struct.pack('=BI', COMMAND_MESSAGE, len(data))
        packet = header + data
        
        disconnected = []
        for client in self.clients:
            try:
                client.sendall(packet)
            except Exception as e:
                disconnected.append(client)
        
        for client in disconnected:
            self.clients.remove(client)
            try:
                client.close()
            except:
                pass
            

    def shutdown(self):
        Log.info("Shutdown Server")
        self.running = False
        if self.server:
            self.server.close()

    def get_data(self):
        if len(self.queue) > 0:
            return self.queue.popleft()
        else:
            return None