# Mappings for each sub-group
# Add in order used in-game
# Keep float/vector/switch param nodes instead of setting values
# Switch RGB to CombineXYZ when any value is less than 0
# When alpha is used for vector, create separate "Param Alpha" value node
# Closure flag in mappings? Check socket for color vs closure?

import traceback
import bpy
from mathutils import Vector

from ..material import *
from ..enums import *
from ..utils import *
from ...utils import *
from ...logger import Log

def create_texture_node(nodes, name, image, srgb):
    node = nodes.new(type="ShaderNodeTexImage")
    node.image = image
    node.image.alpha_mode = 'CHANNEL_PACKED'
    node.image.colorspace_settings.name = "sRGB" if srgb else "Non-Color"
    node.interpolation = "Smart"
    node.label = name
    node.width = 250
    node.hide = True
    return node

def create_scalar_node(nodes, name, value):
    node = nodes.new(type="ShaderNodeValue")
    node.outputs[0].default_value = value
    node.label = name
    node.width = 250
    return node

def create_vector_node(nodes, name, value):
    node = nodes.new(type="ShaderNodeGroup")
    node.node_tree = bpy.data.node_groups.get("FPv4 Vector4")
    node.inputs[0].default_value = value["R"]
    node.inputs[1].default_value = value["G"]
    node.inputs[2].default_value = value["B"]
    node.inputs[3].default_value = value["A"]
    node.label = name
    node.width = 250
    return node

def create_color_node(nodes, name, value):
    node = nodes.new(type="ShaderNodeRGB")
    node.outputs[0].default_value = (value["R"], value["G"], value["B"], value["A"])
    node.label = name
    node.width = 250
    return node

def create_switch_node(nodes, name, value):
    node = nodes.new("ShaderNodeGroup")
    node.node_tree = bpy.data.node_groups.get("FPv4 Switch")
    node.inputs[0].default_value = 1 if value else 0
    node.label = name
    node.width = 250
    return node

def create_uv_node(nodes, uv_map, ref_location):
    uv = nodes.new(type="ShaderNodeUVMap")
    uv.location = ref_location.x - 250, ref_location.y
    uv.uv_map = uv_map
    return uv


def link_texture_node(nodes, links, node, target_node, mappings, x, y):
    node.location = x - 300, y
    node.hide = True
    links.new(node.outputs[0], target_node.inputs[mappings.slot])

    if mappings.alpha_slot:
        links.new(node.outputs[1], target_node.inputs[mappings.alpha_slot])

    if mappings.coords != "UV0":
        uv = create_uv_node(nodes, mappings.coords, node.location)
        links.new(uv.outputs[0], node.inputs[0])

class MaterialImportContext:
    def import_material(self, material_slot, material_data, meta, as_material_data=False):

        if not as_material_data:
            temp_material = material_slot.material
            material_slot.link = 'OBJECT' if self.type in [EExportType.WORLD, EExportType.PREFAB] else 'DATA'
            material_slot.material = temp_material

        material_name = material_data.get("Name")
        material_hash = material_data.get("Hash")
        additional_hash = 0

        texture_data = meta.get("TextureData")
        if texture_data is not None:
            override_material_data = None
            for data in texture_data:
                additional_hash += data.get("Hash")
                if td_override_material := data.get("OverrideMaterial"):
                    override_material_data = td_override_material
                    break

            if override_material_data:
                material_data = override_material_data
                material_name = override_material_data.get("Name")
                material_hash = override_material_data.get("Hash")

        textures = material_data.get("Textures")
        scalars = material_data.get("Scalars")
        vectors = material_data.get("Vectors")
        switches = material_data.get("Switches")
        component_masks = material_data.get("ComponentMasks")

        if texture_data is not None:

            for data in texture_data:
                index = data.get("Index")

                texture_suffix = f"_Texture_{index + 1}" if index > 0 else ""
                spec_suffix = f"_{index + 1}" if index > 0 else ""

                if diffuse := data.get("Diffuse"):
                    replace_or_add_parameter_from_texture(textures, f"Diffuse{texture_suffix}", diffuse)
                if normal := data.get("Normal"):
                    replace_or_add_parameter_from_texture(textures, f"Normals{texture_suffix}", normal)
                if specular := data.get("Specular"):
                    replace_or_add_parameter_from_texture(textures, f"SpecularMasks{spec_suffix}", specular)

        override_parameters = where(self.override_parameters, lambda param: param.get("MaterialNameToAlter").lower() in [material_name.lower(), "global"])
        if override_parameters is not None:
            for parameters in override_parameters:
                additional_hash += parameters.get("Hash")

        if additional_hash != 0:
            material_hash += additional_hash
            material_name += f"_{hash_code(material_hash)}"


        if existing_material := first(bpy.data.materials, lambda mat: mat.get("Hash") == hash_code(material_hash)):
            if not as_material_data:
                material_slot.material = existing_material
                return

        # same name but different hash
        if (name_existing := first(bpy.data.materials, lambda mat: mat.name == material_name)) and name_existing.get("Hash") != material_hash:
            material_name += f"_{hash_code(material_hash)}"

        if not as_material_data and material_slot.material.name.casefold() != material_name.casefold():
            material_slot.material = bpy.data.materials.new(material_name)

        if not as_material_data:
            material_slot.material["Hash"] = hash_code(material_hash)
            material_slot.material["OriginalName"] = material_data.get("Name")

        material = bpy.data.materials.new(material_name) if as_material_data else material_slot.material
        material.use_nodes = True
        material.surface_render_method = "DITHERED"

        nodes = material.node_tree.nodes
        nodes.clear()
        links = material.node_tree.links
        links.clear()

        override_blend_mode = EBlendMode(material_data.get("OverrideBlendMode"))
        base_blend_mode = EBlendMode(material_data.get("BaseBlendMode"))
        translucency_lighting_mode = ETranslucencyLightingMode(material_data.get("TranslucencyLightingMode"))
        shading_model = EMaterialShadingModel(material_data.get("ShadingModel"))
        
        textures = material_data.get("Textures")
        scalars = material_data.get("Scalars")
        vectors = material_data.get("Vectors")
        switches = material_data.get("Switches")
        component_masks = material_data.get("ComponentMasks")

        if override_parameters is not None:
            for parameters in override_parameters:
                for texture in parameters.get("Textures"):
                    replace_or_add_parameter(textures, texture)

                for scalar in parameters.get("Scalars"):
                    replace_or_add_parameter(scalars, scalar)

                for vector in parameters.get("Vectors"):
                    replace_or_add_parameter(vectors, vector)

        output_node = nodes.new(type="ShaderNodeOutputMaterial")
        output_node.location = (200, 0)

        shader_node = nodes.new(type="ShaderNodeGroup")
        shader_node.node_tree = bpy.data.node_groups.get("FPv4 Material Build")

        # TODO: Leaving this in for alternate build node support
        def replace_shader_node(name):
            nonlocal shader_node
            nodes.remove(shader_node)
            shader_node = nodes.new(type="ShaderNodeGroup")
            shader_node.node_tree = bpy.data.node_groups.get(name)

        # for cleaner code sometimes bc stuff gets repetitive
        def set_param(name, value, override_shader=None):

            nonlocal shader_node
            target_node = override_shader or shader_node
            target_node.inputs[name].default_value = value

        def get_node(target_node, slot):
            node_links = target_node.inputs[slot].links
            if node_links is None or len(node_links) == 0:
                return None

            return node_links[0].from_node

        def get_first_node(target_node, slots):
            for slot in slots:
                node_links = target_node.inputs[slot].links
                if len(node_links) > 0:
                    return node_links[0].from_node

            return None

        used_textures = set()
        used_scalars = set()
        used_vectors = set()
        used_component_masks = set()
        used_switches = set()
        
        def texture_param(data, target_mappings, target_node=shader_node):
            try:
                name = data.get("Name")
                path = data.get("Texture").get("Path")
                texture_name = path.split(".")[1]
                srgb = data.get("Texture").get("sRGB")

                mappings = first(target_mappings.textures, lambda x: x.name.casefold() == name.casefold())
                if mappings is None or texture_name in texture_ignore_names:
                    return

                node = create_texture_node(nodes, name, self.import_image(path), srgb)
                x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                if mappings.closure:
                    setup_closure(node, x, y, mappings.slot, target_node, nodes, links)
                else:
                    link_texture_node(nodes, links, node, target_node, mappings, x, y)

                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1

                used_textures.add(name.casefold())
            except Exception:
                traceback.print_exc()

        def scalar_param(data, target_mappings, target_node=shader_node):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.scalars, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    return

                node = create_scalar_node(nodes, name, value)
                x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                node.location = x - 300, y
                node.hide = True
                node.outputs[0].default_value = mappings.value_func(value) if mappings.value_func else value
                links.new(node.outputs[0], target_node.inputs[mappings.slot])

                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0

                used_scalars.add(name.casefold())
            except Exception:
                traceback.print_exc()

        def vector_param(data, target_mappings, target_node=shader_node):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.vectors, lambda x: x.name.casefold() == name.casefold())
                is_vector = mappings is not None

                if not is_vector:
                    mappings = first(target_mappings.colors, lambda x: x.name.casefold() == name.casefold())

                if mappings is None:
                    is_vector = any(vector_param_names, lambda x: x.casefold() in name.casefold()) or any(list(value.values())[0:4], lambda v: v < 0)

                if mappings is None:
                    return

                node = create_vector_node(nodes, name, value) if is_vector else create_color_node(nodes, name, value)
                x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                node.location = x - 300, y
                node.hide = True
                links.new(node.outputs[0], target_node.inputs[mappings.slot])

                if mappings.alpha_slot:
                    target_node.inputs[mappings.alpha_slot].default_value = value["A"]
                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0

                used_vectors.add(name.casefold())
            except Exception:
                traceback.print_exc()

        def component_mask_param(data, target_mappings, target_node=shader_node):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.component_masks, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    return

                node = create_color_node(nodes, name, value)
                x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                node.location = x - 300, y
                node.hide = True
                links.new(node.outputs[0], target_node.inputs[mappings.slot])
                
                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0

                used_component_masks.add(name.casefold())
            except Exception:
                traceback.print_exc()

        def switch_param(data, target_mappings, target_node=shader_node):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.switches, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    return

                node = create_switch_node(nodes, name, value)
                x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                node.location = x - 300, y
                node.hide = True
                links.new(node.outputs[0], target_node.inputs[mappings.slot])

                used_switches.add(name.casefold())
            except Exception:
                traceback.print_exc()

        def handle_default_params(mappings, target_node):
            for texture in mappings.textures:
                if texture.default is not None and get_node(target_node, texture.slot) is None:
                    node = create_texture_node(
                        nodes, texture.slot,
                        bpy.data.images.get(texture.default.name),
                        texture.default.sRGB
                    )
                    x, y = get_socket_pos(target_node, target_node.inputs.find(texture.slot))
                    if texture.closure:
                        setup_closure(node, x, y, texture.slot, target_node, nodes, links)
                    else:
                        link_texture_node(nodes, links, node, target_node, texture, x, y)

                    if texture.switch_slot:
                        target_node.inputs[texture.switch_slot].default_value = 1

            for scalar in mappings.scalars:
                if scalar.default is not None and get_node(target_node, scalar.slot) is None:
                    node = create_scalar_node(nodes, scalar.slot, scalar.default)
                    x, y = get_socket_pos(target_node, target_node.inputs.find(scalar.slot))
                    node.location = x - 300, y
                    node.hide = True
                    links.new(node.outputs[0], target_node.inputs[scalar.slot])

            for vector in mappings.vectors:
                if vector.default is not None and get_node(target_node, vector.slot) is None:
                    value = {"R": vector.default[0], "G": vector.default[1],
                             "B": vector.default[2], "A": vector.default[3]}
                    node = create_vector_node(nodes, vector.slot, value)
                    x, y = get_socket_pos(target_node, target_node.inputs.find(vector.slot))
                    node.location = x - 300, y
                    node.hide = True
                    links.new(node.outputs[0], target_node.inputs[vector.slot])

            for component_mask in mappings.component_masks:
                if component_mask.default is not None and get_node(target_node, component_mask.slot) is None:
                    target_node.inputs[component_mask.slot].default_value = component_mask.default

            for switch in mappings.switches:
                if switch.default is not None and get_node(target_node, switch.slot) is None:
                    node = create_switch_node(nodes, switch.slot, switch.default)
                    x, y = get_socket_pos(target_node, target_node.inputs.find(switch.slot))
                    node.location = x - 300, y
                    node.hide = True
                    links.new(node.outputs[0], target_node.inputs[switch.slot])

        def setup_params(mappings, target_node):
            for texture in textures:
                texture_param(texture, mappings, target_node)
            for scalar in scalars:
                scalar_param(scalar, mappings, target_node)
            for vector in vectors:
                vector_param(vector, mappings, target_node)
            for component_mask in component_masks:
                component_mask_param(component_mask, mappings, target_node)
            for switch in switches:
                switch_param(switch, mappings, target_node)
            handle_default_params(mappings, target_node)

    
        def add_unused_params():
            y = 0

            for texture in textures:
                name = texture.get("Name")
                if name.casefold() in used_textures:
                    continue
                    
                path = texture.get("Texture").get("Path")
                
                try:
                    node = create_texture_node(nodes, name, self.import_image(path), texture.get("Texture").get("sRGB"))
                    node.location = 400, y
                    y -= 50
                except Exception:
                    traceback.print_exc()

            for scalar in scalars:
                name = scalar.get("Name")
                if name.casefold() in used_scalars:
                    continue
                node = create_scalar_node(nodes, name, scalar.get("Value"))
                node.location = 400, y
                y -= 100

            for vector in vectors:
                name = vector.get("Name")
                if name.casefold() in used_vectors:
                    continue
                value = vector.get("Value")
                is_vector = (
                        any(vector_param_names, lambda x: x.casefold() in name.casefold())
                        or any(list(value.values())[0:4], lambda v: v < 0)
                )
                node = create_vector_node(nodes, name, value) if is_vector else create_color_node(nodes, name, value)
                node.location = 400, y
                y -= 200

            for component_mask in component_masks:
                name = component_mask.get("Name")
                if name.casefold() in used_component_masks:
                    continue
                node = create_color_node(nodes, name, component_mask.get("Value"))
                node.location = 400, y
                y -= 200

            for switch in switches:
                name = switch.get("Name")
                if name.casefold() in used_switches:
                    continue
                node = create_switch_node(nodes, name, switch.get("Value"))
                node.location = 400, y
                y -= 125

        all_mappings = find_all_matching_mappings(material_data)

        set_param("AO", self.options.get("AmbientOcclusion"))
        set_param("Cavity", self.options.get("Cavity"))
        set_param("Subsurface Scale", self.options.get("Subsurface"))

        node_position = -200
        previous_node = shader_node

        if len(all_mappings) == 0 or all_mappings[-1].type != ENodeType.NT_Base:
            all_mappings.append(DefaultMappings)

        def add_shader_module(mapping):
            nonlocal node_position, previous_node
            new_node = nodes.new(type="ShaderNodeGroup")
            new_node.node_tree = bpy.data.node_groups.get(mapping.node_name)
            new_node.location = (node_position, 0)
            links.new(new_node.outputs[0], previous_node.inputs[0])
            setup_params(mapping, new_node)
            previous_node = new_node
            node_position -= mapping.node_spacing
            return new_node

        for mapping in all_mappings:
            add_shader_module(mapping)
            if mapping.surface_render_method is not None:
                material.surface_render_method = mapping.surface_render_method
                material.show_transparent_back = mapping.show_transparent_back

        # TODO: MappingCollection.material_changes()?
        # That wouldn't give us the toon outline though because we couldn't call self.add_toon_outline
        if all_mappings[-1].node_name == "FPv4 Base Toon":
            set_param("Brightness", self.options.get("ToonShadingBrightness"), previous_node)
            self.add_toon_outline = True

        # TODO: Part modifier handling? (fur, new toon outline, etc)
        
        add_unused_params()

        links.new(shader_node.outputs[0], output_node.inputs[0])

        # post parameter handling

        if material_name in vertex_crunch_names or get_param(scalars, "HT_CrunchVerts") == 1 or any(toon_outline_names, lambda x: x in material_name):
            self.full_vertex_crunch_materials.append(material)
            return

        if get_param(switches, "Use Vertex Colors for Mask"):
            elements = {
                scalar.get("Name"): scalar.get("Value")
                for scalar in scalars
                if "Hide Element" in scalar.get("Name")
            }
            self.partial_vertex_crunch_materials[material] = elements


    def import_material_standalone(self, data):
        is_object_import = EMaterialImportMethod.OBJECT == EMaterialImportMethod(self.options.get("MaterialImportMethod"))
        materials = data.get("Materials")

        if materials is None:
            return

        if is_object_import:
            self.collection = create_or_get_collection("Materials") if self.options.get("ImportIntoCollection") else bpy.context.scene.collection

        for material in materials:
            name = material.get("Name")
            Log.info(f"Importing Material: {name}")
            if is_object_import:
                bpy.ops.mesh.primitive_cube_add()
                mat_mesh = bpy.context.active_object
                mat_mesh.name = name
                mat_mesh.data.materials.append(bpy.data.materials.new(name))
                self.import_material_new(mat_mesh.material_slots[material.get("Slot")], material, {})
            else:
                self.import_material_new(None, material, {}, True)