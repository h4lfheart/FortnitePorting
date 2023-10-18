import bpy
import os
import json
from enum import Enum
from . import ue_format as ueformat

class MeshType(Enum):
		UEFORMAT = 0
		ACTORX = 1

def get_armature_mesh(obj):
	if obj.type == 'ARMATURE':
		return obj.children[0]

	if obj.type == 'MESH':
		return obj

def append_data():
	addon_dir = os.path.dirname(os.path.splitext(__file__)[0])
	with bpy.data.libraries.load(os.path.join(addon_dir, "fortnite_porting_data.blend")) as (data_from, data_to):
		for node_group in data_from.node_groups:
			if not bpy.data.node_groups.get(node_group):
				data_to.node_groups.append(node_group)

		'''for obj in data_from.objects:
			if not bpy.data.objects.get(obj):
				data_to.objects.append(obj)

		for mat in data_from.materials:
			if not bpy.data.materials.get(mat):
				data_to.materials.append(mat)'''

location_mappings = {
	"Diffuse": (-300, -75),
	"M": (-300, -120),
	"SpecularMasks": (-300, -275),
	"Normals": (-300, -315)
}

texture_mappings = {
	"Diffuse": "Diffuse",
	"M": "M",
	"SpecularMasks": "SpecularMasks",
	"Normals": "Normals"
}

switch_mappings = {
	"SwizzleRoughnessToGreen": "SwizzleRoughnessToGreen",
}

class ImportTask:
	def run(self, response):
		print(json.dumps(response))

		self.assets_folder = response.get("AssetsFolder")
		self.options = response.get("Options")

		append_data()
		datas = response.get("Data")
		for data in datas:
			self.import_data(data)

	def import_data(self, data):
		for mesh in data.get("Meshes"):
			imported_object = self.import_mesh(mesh.get("Path"))
			imported_mesh = get_armature_mesh(imported_object)

			for material in mesh.get("Materials"):
				index = material.get("Slot")
				self.import_material(imported_mesh.material_slots[index], material)

			for override_material in mesh.get("OverrideMaterials"):
				index = override_material.get("Slot")
				if index >= len(imported_mesh.material_slots):
					continue
				self.import_material(imported_mesh.material_slots[index], override_material)

	def import_material(self, material_slot, material_data):
		material = material_slot.material
		material.use_nodes = True
		nodes = material.node_tree.nodes
		nodes.clear()
		links = material.node_tree.links
		links.clear()

		textures = material_data.get("Textures")
		scalars = material_data.get("Scalars")
		vectors = material_data.get("Vectors")
		switches = material_data.get("Switches")
		component_masks = material_data.get("ComponentMasks")

		output_node = nodes.new(type="ShaderNodeOutputMaterial")
		output_node.location = (200, 0)

		shader_node = nodes.new(type="ShaderNodeGroup")
		shader_node.name = "FP Material"
		shader_node.node_tree = bpy.data.node_groups.get(shader_node.name)
		shader_node.inputs["AO"].default_value = self.options.get("AmbientOcclusion")
		shader_node.inputs["Cavity"].default_value = self.options.get("Cavity")
		shader_node.inputs["Subsurface"].default_value = self.options.get("Subsurface")
		links.new(shader_node.outputs[0], output_node.inputs[0])

		def texture_param(data):
			name = data.get("Name")
			path = data.get("Value")

			if (slot := texture_mappings.get(name)) is None:
				return

			node = nodes.new(type="ShaderNodeTexImage")
			node.image = self.import_image(path)
			node.image.alpha_mode = 'CHANNEL_PACKED'
			node.image.colorspace_settings.name = "sRGB" if data.get("sRGB") else "Non-Color"
			node.location = location_mappings[slot]
			node.hide = True
			links.new(node.outputs[0], shader_node.inputs[slot])

		def switch_param(data):
			name = data.get("Name")
			value = data.get("Value")
			if (slot := switch_mappings.get(name)) is None:
				return

			shader_node.inputs[slot].default_value = 1 if value else 0


		for texture in textures:
			texture_param(texture)

		for switch in switches:
			switch_param(switch)

	def import_image(self, path: str):
		path, name = path.split(".")
		if existing := bpy.data.images.get(name):
			return existing

		path = path[1:] if path.startswith("/") else path
		ext = "png"
		texture_path = os.path.join(self.assets_folder, path + "." + ext)

		if not os.path.exists(texture_path):
			return None

		return bpy.data.images.load(texture_path, check_existing=True)

	def import_mesh(self, path: str):
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