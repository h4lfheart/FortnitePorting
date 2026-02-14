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

        unused_parameter_height = 0

        # parameter handlers
        def texture_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                path = data.get("Texture").get("Path")
                texture_name = path.split(".")[1]

                node = nodes.new(type="ShaderNodeTexImage")
                node.image = self.import_image(path)
                node.image.alpha_mode = 'CHANNEL_PACKED'
                node.image.colorspace_settings.name = "sRGB" if data.get("Texture").get("sRGB") else "Non-Color"
                node.interpolation = "Smart"
                node.label = name
                node.width = 250
                node.hide = True

                mappings = first(target_mappings.textures, lambda x: x.name.casefold() == name.casefold())
                if mappings is None or texture_name in texture_ignore_names:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 50
                    else:
                        nodes.remove(node)
                    return

                x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                if mappings.closure:
                    setup_closure(node, x, y, mappings.slot, target_node, nodes, links)
                else:
                    node.location = x - 300, y
                    links.new(node.outputs[0], target_node.inputs[mappings.slot])

                    if mappings.alpha_slot:
                        links.new(node.outputs[1], target_node.inputs[mappings.alpha_slot])
                    if mappings.coords != "UV0":
                        uv = nodes.new(type="ShaderNodeUVMap")
                        uv.location = node.location.x - 250, node.location.y
                        uv.uv_map = mappings.coords
                        links.new(uv.outputs[0], node.inputs[0])

                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1
            except KeyError:
                nodes.remove(node)
                pass
            except Exception:
                traceback.print_exc()

        def scalar_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                node = nodes.new(type="ShaderNodeValue")
                node.outputs[0].default_value = value
                node.label = name
                node.width = 250

                if mappings := first(target_mappings.scalars, lambda x: x.name.casefold() == name.casefold()):
                    x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                    node.location = x - 300, y
                    node.hide = True
                    node.outputs[0].default_value = mappings.value_func(value) if mappings.value_func else value
                    links.new(node.outputs[0], target_node.inputs[mappings.slot])
                else:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 100
                    else:
                        nodes.remove(node)
                    return

                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
            except KeyError:
                nodes.remove(node)
                pass
            except Exception:
                traceback.print_exc()

        def vector_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.vectors, lambda x: x.name.casefold() == name.casefold())
                is_vector = mappings is not None

                if not is_vector:
                    mappings = first(target_mappings.colors, lambda x: x.name.casefold() == name.casefold())

                if mappings is None:
                    is_vector = any(vector_param_names, lambda x: x.casefold() in name.casefold()) or any(list(value.values())[0:4], lambda v: v < 0)

                node = None
                if is_vector:
                    node = nodes.new(type="ShaderNodeGroup")
                    node.node_tree = bpy.data.node_groups.get("FPv4 Vector4")
                    node.inputs[0].default_value = value["R"]
                    node.inputs[1].default_value = value["G"]
                    node.inputs[2].default_value = value["B"]
                    node.inputs[3].default_value = value["A"]
                else:
                    node = nodes.new(type="ShaderNodeRGB")
                    node.outputs[0].default_value = (value["R"], value["G"], value["B"], value["A"])

                node.label = name
                node.width = 250

                if mappings is not None:
                    x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                    node.location = x - 300, y
                    node.hide = True
                    links.new(node.outputs[0], target_node.inputs[mappings.slot])
                else:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 200
                    else:
                        nodes.remove(node)
                    return

                if mappings.alpha_slot: # TODO: How to handle this with connected nodes?
                    target_node.inputs[mappings.alpha_slot].default_value = value["A"]
                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
            except KeyError:
                nodes.remove(node)
                pass
            except Exception:
                traceback.print_exc()

        def component_mask_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                # TODO: Move masks from vectors to component masks in mappings?
                # node = nodes.new(type="ShaderNodeGroup")
                # node.node_tree = bpy.data.node_groups.get("FPv4 ComponentMask")
                # node.inputs[0].default_value = int(value["R"])
                # node.inputs[1].default_value = int(value["G"])
                # node.inputs[2].default_value = int(value["B"])
                # node.inputs[3].default_value = int(value["A"])
                node = nodes.new(type="ShaderNodeRGB") # TODO: switch back to FPv4 ComponentMask node
                node.outputs[0].default_value = (value["R"], value["G"], value["B"], value["A"])
                node.label = name
                node.width = 250

                # Materials that use this:
                # M_F_Tie_Dye_Fashion_Summer_Lime - ClothFuzz and ThinFilm channels
                # M_Med_Soldier_04_Celestial - PanningEmissive and Galaxy channels
                # Pretty much anyone from the seven

                if mappings := first(target_mappings.component_masks, lambda x: x.name.casefold() == name.casefold()):
                    x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                    node.location = x - 300, y
                    node.hide = True
                    links.new(node.outputs[0], target_node.inputs[mappings.slot])
                else:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 200
                    else:
                        nodes.remove(node)
                    return

            except KeyError:
                nodes.remove(node)
                pass
            except Exception:
                traceback.print_exc()

        def switch_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                node = nodes.new("ShaderNodeGroup")
                node.node_tree = bpy.data.node_groups.get("FPv4 Switch")
                node.inputs[0].default_value = 1 if value else 0
                node.label = name
                node.width = 250

                if mappings := first(target_mappings.switches, lambda x: x.name.casefold() == name.casefold()):
                    x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                    node.location = x - 300, y
                    node.hide = True
                    links.new(node.outputs[0], target_node.inputs[mappings.slot])
                else:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 125
                    else:
                        nodes.remove(node)
                    return

            except KeyError:
                nodes.remove(node)
                pass
            except Exception:
                traceback.print_exc()

        # TODO: Refactor, break out node creation to utils and use here and in parameter handlers
        def handle_default_params(mappings, target_node):
            for texture in mappings.textures:
                if texture.default is not None and get_node(target_node, texture.slot) is None:
                    node = nodes.new(type="ShaderNodeTexImage")
                    node.image = bpy.data.images.get(texture.default.name)
                    node.image.alpha_mode = 'CHANNEL_PACKED'
                    node.image.colorspace_settings.name = "sRGB" if texture.default.sRGB else "Non-Color"
                    node.interpolation = "Smart"
                    node.label = texture.slot
                    node.hide = True
                    x, y = get_socket_pos(target_node, target_node.inputs.find(texture.slot))
                    if texture.closure:
                        setup_closure(node, x, y, texture.slot, target_node, nodes, links)
                    else:
                        node.location = x - 300, y
                        links.new(node.outputs[0], target_node.inputs[texture.slot])

                        if texture.alpha_slot:
                            links.new(node.outputs[1], target_node.inputs[texture.alpha_slot])
                        if texture.coords != "UV0":
                            uv = nodes.new(type="ShaderNodeUVMap")
                            uv.location = node.location.x - 250, node.location.y
                            uv.uv_map = texture.coords
                            links.new(uv.outputs[0], node.inputs[0])

                    if texture.switch_slot:
                        target_node.inputs[texture.switch_slot].default_value = 1

            for scalar in mappings.scalars:
                if scalar.default is not None and get_node(target_node, scalar.slot) is None:
                    node = nodes.new(type="ShaderNodeValue")
                    node.outputs[0].default_value = scalar.default
                    node.label = scalar.slot
                    node.width = 250
                    x, y = get_socket_pos(target_node, target_node.inputs.find(scalar.slot))
                    node.location = x - 300, y
                    node.hide = True
                    links.new(node.outputs[0], target_node.inputs[scalar.slot])

            for vector in mappings.vectors:
                if vector.default is not None and get_node(target_node, vector.slot) is None:
                    node = nodes.new(type="ShaderNodeGroup")
                    node.node_tree = bpy.data.node_groups.get("FPv4 Vector4")
                    node.inputs[0].default_value = vector.default[0]
                    node.inputs[1].default_value = vector.default[1]
                    node.inputs[2].default_value = vector.default[2]
                    node.inputs[3].default_value = vector.default[3]
                    node.label = vector.slot
                    node.width = 250
                    x, y = get_socket_pos(target_node, target_node.inputs.find(vector.slot))
                    node.location = x - 300, y
                    node.hide = True
                    links.new(node.outputs[0], target_node.inputs[vector.slot])

            for component_mask in mappings.component_masks:
                if component_mask.default is not None and get_node(target_node, component_mask.slot) is None:
                    target_node.inputs[component_mask.slot].default_value = component_mask.default

            for switch in mappings.switches:
                if switch.default is not None and get_node(target_node, switch.slot) is None:
                    node = nodes.new("ShaderNodeGroup")
                    node.node_tree = bpy.data.node_groups.get("FPv4 Switch")
                    node.inputs[0].default_value = switch.default
                    node.label = switch.slot
                    node.width = 250
                    x, y = get_socket_pos(target_node, target_node.inputs.find(switch.slot))
                    node.location = x - 300, y
                    node.hide = True
                    links.new(node.outputs[0], target_node.inputs[switch.slot])


        def setup_params(mappings, target_node, add_unused_params=False):
            for texture in textures:
                texture_param(texture, mappings, target_node, add_unused_params)

            for scalar in scalars:
                scalar_param(scalar, mappings, target_node, add_unused_params)

            for vector in vectors:
                vector_param(vector, mappings, target_node, add_unused_params)

            for component_mask in component_masks:
                component_mask_param(component_mask, mappings, target_node, add_unused_params)

            for switch in switches:
                switch_param(switch, mappings, target_node, add_unused_params)

            handle_default_params(mappings, target_node)


        all_mappings = find_all_matching_mappings(material_data)

        # TODO: Move to mappings to allow for other build nodes?
        set_param("AO", self.options.get("AmbientOcclusion"))
        set_param("Cavity", self.options.get("Cavity"))
        set_param("Subsurface Scale", self.options.get("Subsurface"))

        node_position = -200
        previous_node = shader_node

        # Add default base layer if no base layer was matched
        if len(all_mappings) == 0 or all_mappings[-1].type != ENodeType.NT_Base:
            all_mappings.append(DefaultMappings)


        def add_shader_module(mapping):
            nonlocal node_position
            nonlocal previous_node
            Log.info(f"Adding node: {mapping.node_name}")
            new_node = nodes.new(type="ShaderNodeGroup")
            new_node.node_tree = bpy.data.node_groups.get(mapping.node_name)
            new_node.location = (node_position, 0)
            links.new(new_node.outputs[0], previous_node.inputs[0])
            setup_params(mapping, new_node, False)
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

        # Temp to add all params for debugging
        # TODO: Only unused on right logic
        setup_params(MappingCollection(), shader_node, True)

        links.new(shader_node.outputs[0], output_node.inputs[0])

        # post parameter handling

        if material_name in vertex_crunch_names or get_param(scalars, "HT_CrunchVerts") == 1 or any(toon_outline_names, lambda x: x in material_name):
            self.full_vertex_crunch_materials.append(material)
            return

        if get_param(switches, "Use Vertex Colors for Mask"):
            elements = {}
            for scalar in scalars:
                name = scalar.get("Name")
                if "Hide Element" not in name:
                    continue

                elements[name] = scalar.get("Value")

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