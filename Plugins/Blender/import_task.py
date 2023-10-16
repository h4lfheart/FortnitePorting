import bpy
import os
from . import ue_format as ueformat
from enum import Enum

class MeshType(Enum):
		UEFORMAT = 0
		ACTORX = 1

class ImportTask:
	def run(self, response):
		print(response)

		self.assets_folder = response.get("AssetsFolder")
		self.options = response.get("Options")

		datas = response.get("Data")
		for data in datas:
			self.import_data(data)

	def import_data(self, data):
		for mesh in data.get("Meshes"):
			self.import_mesh(mesh.get("Path"))

	def import_mesh(self, path: str) -> bpy.types.Object:
	    path = path[1:] if path.startswith("/") else path
	    mesh_path = os.path.join(self.assets_folder, path.split(".")[0])

	    extension = MeshType(self.options.get("MeshExportType"))
	    if extension == MeshType.UEFORMAT:
	    	mesh_path += ".uemodel"
	    elif extension == MeshType.ACTORX:
	    	if os.path.exists(mesh_path + ".psk"):
	        	mesh_path += ".psk"
	    	if os.path.exists(mesh_path + ".pskx"):
	        	mesh_path += ".pskx"

	    return ueformat.import_file(mesh_path)