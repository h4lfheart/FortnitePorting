import bpy
import os
import json
import re
from enum import Enum
from math import radians
from mathutils import Matrix, Vector, Euler, Quaternion
from .ue_format import UEFormatImport, UEModelOptions


class MappingCollection:
    def __init__(self, textures=[], scalars=[], vectors=[], switches=[]):
        self.textures = textures
        self.scalars = scalars
        self.vectors = vectors
        self.switches = switches


class SlotMapping:
    def __init__(self, name, slot=None, alpha_slot=None, switch_slot=None, value_func=None, coords="UV0"):
        self.name = name
        self.slot = name if slot is None else slot
        self.alpha_slot = alpha_slot
        self.switch_slot = switch_slot
        self.value_func = value_func
        self.coords = coords


default_mappings = MappingCollection(
    textures=[
        SlotMapping("Diffuse", "Diffuse"),
        SlotMapping("Background Diffuse", "Background Diffuse", alpha_slot="Background Diffuse Alpha"),
        SlotMapping("BG Diffuse Texture", "Background Diffuse", alpha_slot="Background Diffuse Alpha"),
        SlotMapping("M", "M"),
        SlotMapping("Mask", "M"),
        SlotMapping("SpecularMasks", "SpecularMasks"),
        SlotMapping("SRM", "SpecularMasks"),
        SlotMapping("Specular Mask", "SpecularMasks"),
        SlotMapping("Normals", "Normals"),
        SlotMapping("Normal", "Normals"),
        SlotMapping("Emissive", "Emission"),
        SlotMapping("EmissiveTexture", "Emission")
    ],
    scalars=[
        SlotMapping("RoughnessMin", "Roughness Min"),
        SlotMapping("SpecRoughnessMin", "Roughness Min"),
        SlotMapping("RawRoughnessMin", "Roughness Min"),
        SlotMapping("RoughnessMax", "Roughness Max"),
        SlotMapping("SpecRoughnessMax", "Roughness Max"),
        SlotMapping("RawRoughnessMax", "Roughness Max"),
        SlotMapping("emissive mult", "Emission Strength"),
        SlotMapping("HT_CrunchVerts", "Alpha", value_func=lambda value: 1-value)
    ],
    vectors=[
        SlotMapping("Skin Boost Color And Exponent", "Skin Color", alpha_slot="Skin Boost"),
        SlotMapping("EmissiveMultiplier", "Emission Multiplier")
    ],
    switches=[
        SlotMapping("SwizzleRoughnessToGreen")
    ]
)

layer_mappings = MappingCollection(
    textures=[
        SlotMapping("Diffuse"),
        SlotMapping("SpecularMasks"),
        SlotMapping("Normals"),

        SlotMapping("Diffuse_Texture_2"),
        SlotMapping("SpecularMasks_2"),
        SlotMapping("Normals_Texture_2"),

        SlotMapping("Diffuse_Texture_3"),
        SlotMapping("SpecularMasks_3"),
        SlotMapping("Normals_Texture_3"),

        SlotMapping("Diffuse_Texture_4"),
        SlotMapping("SpecularMasks_4"),
        SlotMapping("Normals_Texture_4"),

        SlotMapping("Diffuse_Texture_5"),
        SlotMapping("SpecularMasks_5"),
        SlotMapping("Normals_Texture_5"),

        SlotMapping("Diffuse_Texture_6"),
        SlotMapping("SpecularMasks_6"),
        SlotMapping("Normals_Texture_6")
    ]
)

toon_mappings = MappingCollection(
    textures=[
        SlotMapping("LitDiffuse"),
        SlotMapping("ShadedDiffuse"),
        SlotMapping("DistanceField_InkLines"),
        SlotMapping("InkLineColor_Texture"),
        SlotMapping("SSC_Texture"),
        SlotMapping("Normals")
    ]
)

valet_mappings = MappingCollection(
    textures=[
        SlotMapping("Diffuse"),
        SlotMapping("Mask", alpha_slot="Mask Alpha"),
        SlotMapping("Decal", alpha_slot="Decal Alpha", coords="UV1"),
        SlotMapping("Normal"),
        SlotMapping("Specular Mask"),
        SlotMapping("Scratch/Grime/EMPTY"),
    ],
    scalars=[
        SlotMapping("Scratch Intensity"),
        SlotMapping("Grime Intensity"),
        SlotMapping("Grime Spec"),
        SlotMapping("Grime Roughness"),

        SlotMapping("Layer 01 Specular"),
        SlotMapping("Layer 01 Metalness"),
        SlotMapping("Layer 01 Roughness Min"),
        SlotMapping("Layer 01 Roughness Max"),
        SlotMapping("Layer 01 Clearcoat"),
        SlotMapping("Layer 01 Clearcoat Roughness Min"),
        SlotMapping("Layer 01 Clearcoat Roughness Max"),

        SlotMapping("Layer 02 Specular"),
        SlotMapping("Layer 02 Metalness"),
        SlotMapping("Layer 02 Roughness Min"),
        SlotMapping("Layer 02 Roughness Max"),
        SlotMapping("Layer 02 Clearcoat"),
        SlotMapping("Layer 02 Clearcoat Roughness Min"),
        SlotMapping("Layer 02 Clearcoat Roughness Max"),

        SlotMapping("Layer 03 Specular"),
        SlotMapping("Layer 03 Metalness"),
        SlotMapping("Layer 03 Roughness Min"),
        SlotMapping("Layer 03 Roughness Max"),
        SlotMapping("Layer 03 Clearcoat"),
        SlotMapping("Layer 03 Clearcoat Roughness Min"),
        SlotMapping("Layer 03 Clearcoat Roughness Max"),

        SlotMapping("Layer 04 Specular"),
        SlotMapping("Layer 04 Metalness"),
        SlotMapping("Layer 04 Roughness Min"),
        SlotMapping("Layer 04 Roughness Max"),
        SlotMapping("Layer 04 Clearcoat"),
        SlotMapping("Layer 04 Clearcoat Roughness Min"),
        SlotMapping("Layer 04 Clearcoat Roughness Max"),
    ],
    vectors=[
        SlotMapping("Scratch Tint"),
        SlotMapping("Grime Tint"),

        SlotMapping("Layer 01 Color"),
        SlotMapping("Layer 02 Color"),
        SlotMapping("Layer 03 Color"),
        SlotMapping("Layer 04 Color"),
    ]
)

glass_mappings = MappingCollection(
    textures=[
        SlotMapping("Color_DarkTint"),
        SlotMapping("Normals"),
    ],
    scalars=[
        SlotMapping("Specular"),
        SlotMapping("Metallic"),
        SlotMapping("Roughness"),
        SlotMapping("Window Tint Amount", "Tint Amount"),
        SlotMapping("Fresnel Exponent"),
        SlotMapping("Fresnel Inner Transparency"),
        SlotMapping("Fresnel Inner Transparency Max Tint"),
        SlotMapping("Fresnel Outer Transparency"),
        SlotMapping("Glass thickness", "Thickness"),
    ],
    vectors=[
        SlotMapping("ColorFront", "Color"),
        SlotMapping("Base Color", "Color"),
    ]
)


class ImportTask:
    def run(self, response):
        print(json.dumps(response))

        self.imported_materials = {}
        self.assets_folder = response.get("AssetsFolder")
        self.options = response.get("Options")

        append_data()
        datas = response.get("Data")
        for data in datas:
            self.import_data(data)

    def import_data(self, data):
        name = data.get("Name")
        type = data.get("Type")
        if self.options.get("ImportCollection"):
            create_collection(name)

        meshes = meshes = data.get("Meshes")
        if type in ["Outfit", "Backpack"]:
            meshes = data.get("OverrideMeshes")
            for mesh in data.get("Meshes"):
                if not any(meshes, lambda override_mesh: override_mesh.get("Type") == mesh.get("Type")):
                    meshes.append(mesh)

        def get_meta(search_props):
            out_props = {}
            for mesh in meshes:
                meta = mesh.get("Meta")
                for search_prop in search_props:
                    if found_key := first(meta.keys(), lambda key: key == search_prop):
                        out_props[found_key] = meta.get(found_key)
            return out_props

        imported_meshes = []
        is_toon = False

        def import_mesh(mesh, parent=None):

            # import mesh
            mesh_type = mesh.get("Type")
            imported_object = self.import_mesh(mesh.get("Path"))
            if parent:
                imported_object.parent = parent
            imported_object.rotation_euler = make_euler(mesh.get("Rotation"))
            imported_object.location = make_vector(mesh.get("Location"), mirror_y=True) * 0.01
            imported_object.scale = make_vector(mesh.get("Scale"))

            imported_mesh = get_armature_mesh(imported_object)
            imported_meshes.append({
                "Skeleton": imported_object,
                "Mesh": imported_mesh,
                "Data": mesh,
                "Meta": mesh.get("Meta")
            })

            # fetch metadata
            match mesh_type:
                case "Body":
                    meta = get_meta(["SkinColor"])
                case "Head":
                    meta = get_meta(["MorphNames", "HatType", "PoseData"])

                    shape_keys = imported_mesh.data.shape_keys
                    if (morph_name := meta.get("MorphNames").get(meta.get("HatType"))) and shape_keys is not None:
                        for key in shape_keys.key_blocks:
                            if key.name.casefold() == morph_name.casefold():
                                key.value = 1.0
                case _:
                    meta = {}

            meta["TextureData"] = mesh.get("TextureData")
            meta["OverrideParameters"] = data.get("OverrideParameters")

            # import mats
            for material in mesh.get("Materials"):
                index = material.get("Slot")
                material_type = self.import_material(imported_mesh.material_slots[index], material, meta)
                if material_type == "FP Toon":
                    nonlocal is_toon
                    is_toon = True

            for override_material in mesh.get("OverrideMaterials"):
                index = override_material.get("Slot")
                if index >= len(imported_mesh.material_slots):
                    continue

                overridden_material = imported_mesh.material_slots[index]
                slots = where(imported_mesh.material_slots,
                              lambda slot: slot.name.casefold() == overridden_material.name.casefold())
                for slot in slots:
                    self.import_material(slot, override_material, meta)

            for variant_override_material in data.get("OverrideMaterials"):
                material_name_to_swap = variant_override_material.get("MaterialNameToSwap")
                slots = where(imported_mesh.material_slots,
                              lambda slot: slot.name.casefold() == material_name_to_swap.casefold())
                for slot in slots:
                    self.import_material(slot, variant_override_material, meta)

            for child in mesh.get("Children"):
                import_mesh(child, imported_object)

        for mesh in meshes:
            import_mesh(mesh)

        if type == "Outfit" and self.options.get("MergeSkeletons"):
            master_skeleton = merge_skeletons(imported_meshes)
            master_mesh = get_armature_mesh(master_skeleton)

            if is_toon:
                master_mesh.data.materials.append(bpy.data.materials.get("M_FP_Outline"))

                solidify = master_mesh.modifiers.new(name="Outline", type='SOLIDIFY')
                solidify.thickness = 0.001
                solidify.offset = 0
                solidify.thickness_clamp = 5.0
                solidify.use_rim = False
                solidify.use_flip_normals = True
                solidify.material_offset = len(master_mesh.data.materials) - 1

    def import_material(self, material_slot, material_data, meta_data):
        material_name = material_data.get("Name")
        material_hash = material_data.get("Hash")
        additional_hash = 0

        texture_data = meta_data.get("TextureData")
        if texture_data:
            for data in texture_data:
                additional_hash += data.get("Hash")

        override_parameters = where(meta_data.get("OverrideParameters"),
                                    lambda param: param.get("MaterialNameToAlter") == material_name)
        if override_parameters:
            for parameters in override_parameters:
                additional_hash += parameters.get("Hash")

        if additional_hash != 0:
            material_name += f"_{hash_code(additional_hash)}"
            material_hash += additional_hash

        if existing := self.imported_materials.get(material_hash):
            material_slot.material = existing
            return

        if material_slot.material.name.casefold() != material_name.casefold():
            material_slot.material = bpy.data.materials.new(material_name)

        self.imported_materials[material_hash] = material_slot.material
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

        if texture_data:
            for texture_data_inst in texture_data:
                replace_or_add_parameter(textures, texture_data_inst.get("Diffuse"))
                replace_or_add_parameter(textures, texture_data_inst.get("Normal"))
                replace_or_add_parameter(textures, texture_data_inst.get("Specular"))

        if override_parameters:
            for override_parameter in override_parameters:
                for texture in override_parameter.get("Textures"):
                    replace_or_add_parameter(textures, texture)

                for scalar in override_parameter.get("Scalars"):
                    replace_or_add_parameter(scalars, scalar)

                for vector in override_parameter.get("Vectors"):
                    replace_or_add_parameter(vectors, vector)

        output_node = nodes.new(type="ShaderNodeOutputMaterial")
        output_node.location = (200, 0)

        shader_node = nodes.new(type="ShaderNodeGroup")
        shader_node.node_tree = bpy.data.node_groups.get("FP Material")

        # parameters
        hide_element_values = {}
        unused_parameter_offset = 0
        socket_mappings = default_mappings

        def get_param(source, name):
            found = first(source, lambda param: param.get("Name") == name)
            if found is None:
                return None
            return found.get("Value")

        def get_param_multiple(source, names):
            found = first(source, lambda param: param.get("Name") in names)
            if found is None:
                return None
            return found.get("Value")

        def texture_param(data):
            try:
                name = data.get("Name")
                path = data.get("Value")

                node = nodes.new(type="ShaderNodeTexImage")
                node.image = self.import_image(path)
                node.image.alpha_mode = 'CHANNEL_PACKED'
                node.image.colorspace_settings.name = "sRGB" if data.get("sRGB") else "Non-Color"
                node.interpolation = "Smart"
                node.hide = True

                if (mappings := first(socket_mappings.textures, lambda x: x.name == name)) is None:
                    nonlocal unused_parameter_offset
                    node.label = name
                    node.location = 400, unused_parameter_offset
                    unused_parameter_offset -= 50
                    return

                x, y = get_socket_pos(shader_node, shader_node.inputs.find(mappings.slot))
                node.location = x - 300, y
                links.new(node.outputs[0], shader_node.inputs[mappings.slot])

                if mappings.alpha_slot:
                    links.new(node.outputs[1], shader_node.inputs[mappings.alpha_slot])
                if mappings.switch_slot:
                    shader_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
                if mappings.coords != "UV0":
                    uv = nodes.new(type="ShaderNodeUVMap")
                    uv.location = node.location.x - 250, node.location.y
                    uv.uv_map = mappings.coords
                    links.new(uv.outputs[0], node.inputs[0])
            except Exception as e:
                print(e)

        def scalar_param(data):
            try:
                name = data.get("Name")
                value = data.get("Value")

                if "Hide Element" in name:
                    hide_element_values[name] = value
                    return

                if (mappings := first(socket_mappings.scalars, lambda x: x.name == name)) is None:
                    nonlocal unused_parameter_offset
                    node = nodes.new(type="ShaderNodeValue")
                    node.outputs[0].default_value = value
                    node.label = name
                    node.width = 250
                    node.location = 400, unused_parameter_offset
                    unused_parameter_offset -= 100
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                shader_node.inputs[mappings.slot].default_value = value
                if mappings.switch_slot:
                    shader_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
            except Exception as e:
                print(e)

        def vector_param(data):
            try:
                name = data.get("Name")
                value = data.get("Value")

                if (mappings := first(socket_mappings.vectors, lambda x: x.name == name)) is None:
                    nonlocal unused_parameter_offset
                    node = nodes.new(type="ShaderNodeRGB")
                    node.outputs[0].default_value = (value["R"], value["G"], value["B"], value["A"])
                    node.label = name
                    node.width = 250
                    node.location = 400, unused_parameter_offset
                    unused_parameter_offset -= 200
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                shader_node.inputs[mappings.slot].default_value = (value["R"], value["G"], value["B"], 1.0)
                if mappings.alpha_slot:
                    shader_node.inputs[mappings.alpha_slot].default_value = value["A"]
                if mappings.switch_slot:
                    shader_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
            except Exception as e:
                print(e)

        def switch_param(data):
            try:
                name = data.get("Name")
                value = data.get("Value")

                if (mappings := first(socket_mappings.switches, lambda x: x.name == name)) is None:
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                shader_node.inputs[mappings.slot].default_value = 1 if value else 0
            except Exception as e:
                print(e)

        if any(["Use 2 Layers", "Use 3 Layers", "Use 4 Layers", "Use 5 Layers", "Use 6 Layers"
                                                                                "Use 2 Materials", "Use 3 Materials",
                "Use 4 Materials", "Use 5 Materials", "Use 6 Materials"], lambda x: get_param(switches, x)):
            shader_node.node_tree = bpy.data.node_groups.get("FP Layer")
            socket_mappings = layer_mappings

        if any(["LitDiffuse", "ShadedDiffuse"], lambda x: get_param(textures, x)):
            shader_node.node_tree = bpy.data.node_groups.get("FP Toon")
            socket_mappings = toon_mappings

        if material_data.get("AbsoluteParent") == "M_FN_Valet_Master":
            shader_node.node_tree = bpy.data.node_groups.get("FP Valet")
            socket_mappings = valet_mappings
            
        if material_data.get("UseGlassMaterial"):
            shader_node.node_tree = bpy.data.node_groups.get("FP Glass")
            socket_mappings = glass_mappings
            material.blend_method = "BLEND"
            material.shadow_method = "NONE"
            material.show_transparent_back = False

        for texture in textures:
            texture_param(texture)

        for scalar in scalars:
            scalar_param(scalar)

        for vector in vectors:
            vector_param(vector)

        for switch in switches:
            switch_param(switch)

        links.new(shader_node.outputs[0], output_node.inputs[0])

        if material_name in ["MI_VertexCrunch", "M_VertexCrunch"]:
            material.blend_method = "CLIP"
            material.shadow_method = "CLIP"
            shader_node.inputs["Alpha"].default_value = 0.0
            return

        if shader_node.node_tree.name == "FP Material":
            material.blend_method = "CLIP"
            material.shadow_method = "CLIP"

            shader_node.inputs["AO"].default_value = self.options.get("AmbientOcclusion")
            shader_node.inputs["Cavity"].default_value = self.options.get("Cavity")
            shader_node.inputs["Subsurface"].default_value = self.options.get("Subsurface")

            # find better detection to do this
            '''if (diffuse_links := shader_node.inputs["Diffuse"].links) and len(diffuse_links) > 0:
				diffuse_node = diffuse_links[0].from_node
				links.new(diffuse_node.outputs[1], shader_node.inputs["Alpha"])'''

            if (skin_color := meta_data.get("SkinColor")) and skin_color["A"] != 0:
                shader_node.inputs["Skin Color"].default_value = (
                    skin_color["R"], skin_color["G"], skin_color["B"], 1.0)
                shader_node.inputs["Skin Boost"].default_value = skin_color["A"]

            # only for master character mat bruh
            '''if not (get_param(switches, "Emissive") or get_param(switches, "UseBasicEmissive") or get_param(switches, "UseAdvancedEmissive")):
                shader_node.inputs["Emission Strength"].default_value = 0'''

            if get_param(textures, "SRM"):
                shader_node.inputs["SwizzleRoughnessToGreen"].default_value = 1

            if get_param(switches, "Use Vertex Colors for Mask"):
                color_node = nodes.new(type="ShaderNodeVertexColor")
                color_node.location = [-400, -560]
                color_node.layer_name = "COL0"

                mask_node = nodes.new("ShaderNodeGroup")
                mask_node.node_tree = bpy.data.node_groups.get("FP Vertex Alpha")
                mask_node.location = [-200, -560]

                links.new(color_node.outputs[0], mask_node.inputs[0])
                links.new(mask_node.outputs[0], shader_node.inputs["Alpha"])

                for name, value in hide_element_values.items():
                    if input := mask_node.inputs.get(name.replace("Hide ", "")):
                        input.default_value = int(value)

            emission_slot = shader_node.inputs["Emission Color"]
            emission_crop_params = [
                "EmissiveUVs_RG_UpperLeftCorner_BA_LowerRightCorner",
                "Emissive Texture UVs RG_TopLeft BA_BottomRight",
                "Emissive 2 UV Positioning (RG)UpperLeft (BA)LowerRight",
                "EmissiveUVPositioning (RG)UpperLeft (BA)LowerRight"
            ]

            if (crop_bounds := get_param_multiple(vectors, emission_crop_params)) and get_param(switches,
                                                                                                "CroppedEmissive") and len(
                    emission_slot.links) > 0:
                emission_node = emission_slot.links[0].from_node

                crop_texture_node = nodes.new("ShaderNodeGroup")
                crop_texture_node.node_tree = bpy.data.node_groups.get("FP Texture Cropping")
                crop_texture_node.location = emission_node.location + Vector((-200, 25))
                crop_texture_node.inputs["Left"].default_value = crop_bounds.get('R')
                crop_texture_node.inputs["Top"].default_value = crop_bounds.get('G')
                crop_texture_node.inputs["Right"].default_value = crop_bounds.get('B')
                crop_texture_node.inputs["Bottom"].default_value = crop_bounds.get('A')
                links.new(crop_texture_node.outputs[0], emission_node.inputs[0])

        if shader_node.node_tree.name == "FP Toon":
            shader_node.inputs["Brightness"].default_value = self.options.get("ToonBrightness")

        return shader_node.node_tree.name

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
        mesh_path = os.path.join(self.assets_folder, path.split(".")[0] + ".uemodel")
        
        options = UEModelOptions(scale_down=self.options.get("ScaleDown"),
                                 reorient_bones=self.options.get("ReorientBones"),
                                 bone_length=self.options.get("BoneLength"))

        return UEFormatImport(options).import_file(mesh_path)


class HatType(Enum):
    HeadReplacement = 0
    Cap = 1
    Mask = 2
    Helmet = 3
    Hat = 4


def get_socket_pos(node, index):
    start_y = -80
    offset_y = -22
    return node.location.x, node.location.y + start_y + offset_y * index


def hash_code(num):
    return hex(abs(num))[2:]


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

        for mat in data_from.materials:
            if not bpy.data.materials.get(mat):
                data_to.materials.append(mat)

        '''for obj in data_from.objects:
			if not bpy.data.objects.get(obj):
				data_to.objects.append(obj)'''


def create_collection(name):
    if name in bpy.context.view_layer.layer_collection.children:
        bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children.get(name)
        return
    bpy.ops.object.select_all(action='DESELECT')

    new_collection = bpy.data.collections.new(name)
    bpy.context.scene.collection.children.link(new_collection)
    bpy.context.view_layer.active_layer_collection = bpy.context.view_layer.layer_collection.children.get(
        new_collection.name)


def constraint_object(child: bpy.types.Object, parent: bpy.types.Object, bone: str, rot=[0, radians(90), 0]):
    constraint = child.constraints.new('CHILD_OF')
    constraint.target = parent
    constraint.subtarget = bone
    child.rotation_mode = 'XYZ'
    child.rotation_euler = rot
    constraint.inverse_matrix = Matrix()


def make_vector(data, mirror_y=False):
    return Vector((data.get("X"), data.get("Y") * (-1 if mirror_y else 1), data.get("Z")))


def make_quat(data):
    return Quaternion((data.get("W"), data.get("X"), data.get("Y"), data.get("Z")))


def make_euler(data):
    return Euler((radians(data.get("Roll")), -radians(data.get("Pitch")), -radians(data.get("Yaw"))))


def first(target, expr, default=None):
    if not target:
        return None
    filtered = filter(expr, target)

    return next(filtered, default)


def where(target, expr):
    if not target:
        return None
    filtered = filter(expr, target)

    return list(filtered)


def any(target, expr):
    if not target:
        return None

    filtered = list(filter(expr, target))
    return len(filtered) > 0


def get_case_insensitive(source, string):
    for item in source:
        if item.name.casefold() == string.casefold():
            return item


def replace_or_add_parameter(list, replace_item):
    if replace_item is None:
        return
    for index, item in enumerate(list):
        if item is None:
            continue

        if item.get("Name") == replace_item.get("Name"):
            list[index] = replace_item

    if not any(list, lambda x: x.get("Name") == replace_item.get("Name")):
        list.append(replace_item)


def merge_skeletons(parts):
    bpy.ops.object.select_all(action='DESELECT')

    merge_parts = []
    constraint_parts = []

    for part in parts:
        if (meta := part.get("Meta")) and meta.get("AttachToSocket") and meta.get("Socket") not in ["Face", None]:
            constraint_parts.append(part)
        else:
            merge_parts.append(part)

    # merge skeletons
    for part in merge_parts:
        data = part.get("Data")
        mesh_type = data.get("Type")
        skeleton = part.get("Skeleton")

        if mesh_type == "Body":
            bpy.context.view_layer.objects.active = skeleton

        skeleton.select_set(True)

    bpy.ops.object.join()
    master_skeleton = bpy.context.active_object
    bpy.ops.object.select_all(action='DESELECT')

    # merge meshes
    for part in merge_parts:
        data = part.get("Data")
        mesh_type = data.get("Type")
        mesh = part.get("Mesh")

        if mesh_type == "Body":
            bpy.context.view_layer.objects.active = mesh

        mesh.select_set(True)

    bpy.ops.object.join()
    bpy.ops.object.select_all(action='DESELECT')

    # rebuild master bone tree
    bone_tree = {}
    for bone in master_skeleton.data.bones:
        try:
            bone_reg = re.sub(".\d\d\d", "", bone.name)
            parent_reg = re.sub(".\d\d\d", "", bone.parent.name)
            bone_tree[bone_reg] = parent_reg
        except AttributeError:
            pass

    bpy.context.view_layer.objects.active = master_skeleton
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.armature.select_all(action='DESELECT')
    bpy.ops.object.select_pattern(pattern="*.[0-9][0-9][0-9]")
    bpy.ops.armature.delete()

    skeleton_bones = master_skeleton.data.edit_bones

    for bone, parent in bone_tree.items():
        if target_bone := skeleton_bones.get(bone):
            target_bone.parent = skeleton_bones.get(parent)

    bpy.ops.object.mode_set(mode='OBJECT')

    # constraint meshes
    for part in constraint_parts:
        skeleton = part.get("Skeleton")
        meta = part.get("Meta")
        socket = meta.get("Socket")
        if socket is None:
            return

        if socket.casefold() == "hat":
            socket = "head"

        constraint_object(skeleton, master_skeleton, socket)

    return master_skeleton
