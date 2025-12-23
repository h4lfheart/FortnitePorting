
import traceback
import bpy
from mathutils import Vector

from ..mappings import *
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
                if diffuse := data.get("Diffuse"):
                    replace_or_add_parameter(textures, diffuse)
                if normal := data.get("Normal"):
                    replace_or_add_parameter(textures, normal)
                if specular := data.get("Specular"):
                    replace_or_add_parameter(textures, specular)
        
        override_parameters = where(self.override_parameters, lambda param: param.get("MaterialNameToAlter") in [material_name, "Global"])
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
        shader_node.node_tree = bpy.data.node_groups.get("FPv3 Material Lite" if self.type in lite_shader_types else "FPv3 Material")

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
                node.hide = True

                mappings = first(target_mappings.textures, lambda x: x.name.casefold() == name.casefold())
                if mappings is None or texture_name in texture_ignore_names:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node.label = name
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 50
                    else:
                        nodes.remove(node)
                    return

                x, y = get_socket_pos(target_node, target_node.inputs.find(mappings.slot))
                node.location = x - 300, y
                links.new(node.outputs[0], target_node.inputs[mappings.slot])

                if mappings.alpha_slot:
                    links.new(node.outputs[1], target_node.inputs[mappings.alpha_slot])
                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
                if mappings.coords != "UV0":
                    uv = nodes.new(type="ShaderNodeUVMap")
                    uv.location = node.location.x - 250, node.location.y
                    uv.uv_map = mappings.coords
                    links.new(uv.outputs[0], node.inputs[0])
            except KeyError:
                nodes.remove(node)
                pass
            except Exception:
                traceback.print_exc()

        def scalar_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.scalars, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node = nodes.new(type="ShaderNodeValue")
                        node.outputs[0].default_value = value
                        node.label = name
                        node.width = 250
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 100
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                target_socket = target_node.inputs[mappings.slot]

                match target_socket.type:
                    case "INT":
                        target_socket.default_value = int(value)
                    case "BOOL":
                        target_socket.default_value = int(value) == 1
                    case _:
                        target_socket.default_value = value
                    
                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
            except KeyError as e:
                pass
            except Exception:
                traceback.print_exc()

        def vector_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.vectors, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node = nodes.new(type="ShaderNodeRGB")
                        node.outputs[0].default_value = (value["R"], value["G"], value["B"], value["A"])
                        node.label = name
                        node.width = 250
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 200
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                if isinstance(target_node.inputs[mappings.slot], bpy.types.NodeSocketColor):
                    target_node.inputs[mappings.slot].default_value = (value["R"], value["G"], value["B"], 1.0)
                else:
                    target_node.inputs[mappings.slot].default_value = (value["R"], value["G"], value["B"])
                if mappings.alpha_slot:
                    target_node.inputs[mappings.alpha_slot].default_value = value["A"]
                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
            except KeyError:
                pass
            except Exception:
                traceback.print_exc()

        def component_mask_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.component_masks, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node = nodes.new(type="ShaderNodeRGB")
                        node.outputs[0].default_value = (value["R"], value["G"], value["B"], value["A"])
                        node.label = name
                        node.width = 250
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 200
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                target_node.inputs[mappings.slot].default_value = (value["R"], value["G"], value["B"], value["A"])
            except KeyError:
                pass
            except Exception:
                traceback.print_exc()

        def switch_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                value = data.get("Value")

                mappings = first(target_mappings.switches, lambda x: x.name.casefold() == name.casefold())
                if mappings is None:
                    if add_unused_params:
                        nonlocal unused_parameter_height
                        node = nodes.new("ShaderNodeGroup")
                        node.node_tree = bpy.data.node_groups.get("FPv3 Switch")
                        node.inputs[0].default_value = 1 if value else 0
                        node.label = name
                        node.width = 250
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 125
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                target_socket = target_node.inputs[mappings.slot]
                match target_socket.type:
                    case "INT":
                        target_socket.default_value = 1 if value else 0
                    case "BOOL":
                        target_socket.default_value = value
            except KeyError:
                pass
            except Exception:
                traceback.print_exc()

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

        def move_texture_node(target_node, slot_name, source_node=None):
            source = shader_node if source_node is None else source_node
            if texture_node := get_node(source, slot_name):
                x, y = get_socket_pos(target_node, target_node.inputs.find(slot_name))
                texture_node.location = x - 300, y
                links.new(texture_node.outputs[0], target_node.inputs[slot_name])
                
            links.new(target_node.outputs[slot_name], source.inputs[slot_name])

        def connect_or_add_default_texture(target_node, target_slot, texture_name, sRGB=True, pre_node_link=None):
            texture_node = get_node(target_node, target_slot)
            if texture_node is None:
                texture_node = nodes.new(type="ShaderNodeTexImage")
                texture_node.image = bpy.data.images.get(texture_name)
                texture_node.image.alpha_mode = 'CHANNEL_PACKED'
                texture_node.image.colorspace_settings.name = "sRGB" if sRGB else "Non-Color"
                texture_node.interpolation = "Smart"
                texture_node.hide = True
    
                x, y = get_socket_pos(target_node, target_node.inputs.find(target_slot))
                texture_node.location = x - 300, y
                links.new(texture_node.outputs[0], target_node.inputs[target_slot])
                
            if pre_node_link is not None:
                links.new(pre_node_link, texture_node.inputs[0])

        def connect_texture_uvs(target_node, slot_name, uv_output):
            if texture_node := get_node(target_node, slot_name):
                links.new(uv_output, texture_node.inputs[0])

        # decide which material type and mappings to use
        socket_mappings = default_mappings
        base_material_path = material_data.get("BaseMaterialPath")

        if get_param_multiple(switches, layer_switch_names) and get_param_multiple(textures, extra_layer_names):
            replace_shader_node("FPv3 Layer")
            socket_mappings = layer_mappings

            set_param("Is Transparent", override_blend_mode is not EBlendMode.BLEND_Opaque)

        if get_param_multiple(textures, toon_texture_names) or get_param_multiple(vectors, toon_vector_names):
            replace_shader_node("FPv3 Toon")
            socket_mappings = toon_mappings

        if "M_FN_Valet_Master" in base_material_path:
            replace_shader_node("FPv3 Valet")
            socket_mappings = valet_mappings

        is_glass = material_data.get("PhysMaterialName") == "Glass" or any(glass_master_names, lambda x: x in base_material_path) or (base_blend_mode is EBlendMode.BLEND_Translucent and translucency_lighting_mode in [ETranslucencyLightingMode.TLM_SurfacePerPixelLighting, ETranslucencyLightingMode.TLM_VolumetricPerVertexDirectional])
        if is_glass:
            replace_shader_node("FPv3 Glass")
            socket_mappings = glass_mappings

            material.surface_render_method = "BLENDED"
            material.show_transparent_back = False

        is_trunk = get_param(switches, "IsTrunk")
        if is_trunk:
            socket_mappings = trunk_mappings

        is_foliage = base_blend_mode is EBlendMode.BLEND_Masked and shading_model in [EMaterialShadingModel.MSM_TwoSidedFoliage, EMaterialShadingModel.MSM_Subsurface]
        if is_foliage and not is_trunk:
            replace_shader_node("FPv3 Foliage")
            socket_mappings = foliage_mappings

        if "MM_BeanCharacter_Body" in base_material_path:
            replace_shader_node("FPv3 Bean Base")
            socket_mappings = bean_base_mappings
            
        if "MM_BeanCharacter_Costume" in base_material_path:
            replace_shader_node("FPv3 Bean Costume")
            socket_mappings = bean_head_costume_mappings if meta.get("IsHead") else bean_costume_mappings

        if any(eye_names, lambda eye_mat_name: eye_mat_name in base_material_path) or get_param(scalars, "Eye Cornea IOR") is not None:
            replace_shader_node("FPv3 3L Eyes")
            socket_mappings = eye_mappings

        if "M_HairParent_2023" in base_material_path or get_param(textures, "Hair Mask") is not None:
            replace_shader_node("FPv3 Hair")
            socket_mappings = hair_mappings

        setup_params(socket_mappings, shader_node, True)

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

        match shader_node.node_tree.name:
            case "FPv3 Material":
                # PRE FX START
                pre_fx_node = nodes.new(type="ShaderNodeGroup")
                pre_fx_node.node_tree = bpy.data.node_groups.get("FPv3 Pre FX")
                pre_fx_node.location = -600, -700
                setup_params(socket_mappings, pre_fx_node, False)

                thin_film_node = get_node(shader_node, "Thin Film Texture")
                if get_param_multiple(switches, ["Use Thin Film", "UseThinFilm"]) or "M_ReconExpert_FNCS_Parent" in base_material_path:
                    connect_or_add_default_texture(shader_node, "Thin Film Texture", "T_ThinFilm_Spectrum_COLOR", True, pre_fx_node.outputs["Thin Film UV"])
                elif thin_film_node is not None:
                    nodes.remove(thin_film_node)

                cloth_fuzz_node = get_node(shader_node, "ClothFuzz Texture")
                if get_param_multiple(switches, ["Use Cloth Fuzz", "UseClothFuzz"]):
                    connect_or_add_default_texture(shader_node, "ClothFuzz Texture", "T_Fuzz_MASK", True, pre_fx_node.outputs["Cloth UV"])
                elif cloth_fuzz_node is not None:
                    nodes.remove(cloth_fuzz_node)

                if flipbook_node := get_node(shader_node, "Flipbook Color"):
                    links.new(pre_fx_node.outputs["Flipbook UV"], flipbook_node.inputs[0])
                    links.new(pre_fx_node.outputs["Flipbook Mask"], shader_node.inputs["Flipbook Mask"])

                if emissive_distance_field_node := get_node(shader_node, "EmissiveDistanceField"):
                    links.new(pre_fx_node.outputs["Emissive Distance Field UV"], emissive_distance_field_node.inputs[0])

                if ice_gradient_node := get_node(shader_node, "IceGradient"):
                    links.new(pre_fx_node.outputs["Ice UV"], ice_gradient_node.inputs[0])
                    links.new(pre_fx_node.outputs["Ice UV"], shader_node.inputs["Ice UV"])
                    
                if not any(pre_fx_node.outputs, lambda output: len(output.links) > 0):
                    for input in pre_fx_node.inputs:
                        if len(input.links) > 0:
                            nodes.remove(input.links[0].from_node)
                            
                    nodes.remove(pre_fx_node)
                    
                # PRE FX END
                
                set_param("AO", self.options.get("AmbientOcclusion"))
                set_param("Cavity", self.options.get("Cavity"))
                set_param("Subsurface", self.options.get("Subsurface"))

                if override_blend_mode == EBlendMode.BLEND_Masked and (diffuse_links := shader_node.inputs["Diffuse"].links) and len(diffuse_links) > 0 and (diffuse_node := diffuse_links[0].from_node):
                    links.new(diffuse_node.outputs[1], shader_node.inputs["Alpha"])

                if (skin_color := meta.get("SkinColor")) and skin_color["A"] != 0:
                    set_param("Skin Color", (skin_color["R"], skin_color["G"], skin_color["B"], 1.0))
                    set_param("Skin Boost", skin_color["A"])

                if all(get_params(switches, emissive_toggle_names), lambda bool: bool is False):
                    set_param("Emission Strength", 0)

                if get_param(textures, "SRM"):
                    set_param("SwizzleRoughnessToGreen", 1)

                emission_slot = shader_node.inputs["Emission"]
                if (crop_bounds := get_param_multiple(vectors, emissive_crop_vector_names)) and get_param_multiple(switches, emissive_crop_switch_names) and len(emission_slot.links) > 0:
                    emission_node = emission_slot.links[0].from_node
                    emission_node.extension = "CLIP"
    
                    crop_texture_node = nodes.new("ShaderNodeGroup")
                    crop_texture_node.node_tree = bpy.data.node_groups.get("FPv3 Texture Cropping")
                    crop_texture_node.location = emission_node.location + Vector((-200, 25))
                    crop_texture_node.inputs["Left"].default_value = crop_bounds.get('R')
                    crop_texture_node.inputs["Top"].default_value = crop_bounds.get('G')
                    crop_texture_node.inputs["Right"].default_value = crop_bounds.get('B')
                    crop_texture_node.inputs["Bottom"].default_value = crop_bounds.get('A')
                    links.new(crop_texture_node.outputs[0], emission_node.inputs[0])

                if get_param(switches, "Modulate Emissive with Diffuse"):
                    diffuse_node = shader_node.inputs["Diffuse"].links[0].from_node
                    links.new(diffuse_node.outputs[0], shader_node.inputs["Emission Multiplier"])

                if get_param(switches, "Use Engine Colorized GMap"):
                    gmap_node = nodes.new(type="ShaderNodeGroup")
                    gmap_node.node_tree = bpy.data.node_groups.get(".FPv3 GMap Material")
                    gmap_node.location = -1100, -69
                    if len(shader_node.inputs["Diffuse"].links) > 0:
                        nodes.remove(shader_node.inputs["Diffuse"].links[0].from_node)
                    links.new(gmap_node.outputs[0], shader_node.inputs[0])
                    setup_params(gmap_material_mappings, gmap_node)

                if get_param(switches, "useGmapGradientLayers"):
                    gradient_node = nodes.new(type="ShaderNodeGroup")
                    gradient_node.node_tree = bpy.data.node_groups.get("FPv3 Gradient")
                    gradient_node.location = -500, 0
                    nodes.remove(shader_node.inputs["Diffuse"].links[0].from_node)
                    links.new(gradient_node.outputs[0], shader_node.inputs[0])

                    gmap_node = nodes.new(type="ShaderNodeGroup")
                    gmap_node.node_tree = bpy.data.node_groups.get(".FPv3 GMap")
                    gmap_node.location = -1120, -240
                    setup_params(gmap_mappings, gmap_node, False)

                    setup_params(gradient_mappings, gradient_node)

                    for item in gradient_node.node_tree.interface.items_tree:
                        if item.name != "Colors":
                            continue

                        panel_items = item.interface_items
                        for panel_item in panel_items:
                            item_links = gradient_node.inputs[panel_item.name].links
                            if len(item_links) == 0:
                                continue
                            links.new(gmap_node.outputs[0], item_links[0].from_node.inputs[0])

                if eye_texture_data := get_param_info(textures, "EyeTexture"):
                    eye_texture_node = nodes.new(type="ShaderNodeTexImage")
                    eye_texture_node.image = self.import_image(eye_texture_data.get("Texture").get("Path"))
                    eye_texture_node.image.alpha_mode = 'CHANNEL_PACKED'
                    eye_texture_node.image.colorspace_settings.name = "sRGB" if eye_texture_data.get("Texture").get("sRGB") else "Non-Color"
                    eye_texture_node.interpolation = "Smart"
                    eye_texture_node.hide = True
                    eye_texture_node.location = [-500, -75]

                    uv_map_node = nodes.new(type="ShaderNodeUVMap")
                    uv_map_node.location = [-700, 25]
                    uv_map_node.uv_map = "UV1"

                    links.new(uv_map_node.outputs[0], eye_texture_node.inputs[0])

                    mix_node = nodes.new(type="ShaderNodeMixRGB")
                    mix_node.location = [-200, 75]

                    links.new(eye_texture_node.outputs[0], mix_node.inputs[2])

                    compare_node = nodes.new(type="ShaderNodeMath")
                    compare_node.operation = 'COMPARE'
                    compare_node.hide = True
                    compare_node.location = [-500, 100]
                    compare_node.inputs[1].default_value = 0.510
                    links.new(uv_map_node.outputs[0], compare_node.inputs[0])
                    links.new(compare_node.outputs[0], mix_node.inputs[0])

                    diffuse_node = shader_node.inputs["Diffuse"].links[0].from_node
                    diffuse_node.location = [-500, 0]
                    links.new(diffuse_node.outputs[0], mix_node.inputs[1])
                    links.new(mix_node.outputs[0], shader_node.inputs["Diffuse"])
                    
                if diffuse_node := get_node(shader_node, "Diffuse"):
                    nodes.active = diffuse_node

                if "Elastic_Master" in base_material_path and "_Head_" not in base_material_path:
                    superhero_node = nodes.new(type="ShaderNodeGroup")
                    superhero_node.node_tree = bpy.data.node_groups.get("FPv3 Superhero")
                    superhero_node.location = -600, 0
                    setup_params(superhero_mappings, superhero_node, False)
                    
                    base_normal = superhero_node.inputs["Normals"].links[0].from_node
                    if len(superhero_node.inputs["PrimaryNormal"].links) == 0:
                        links.new(base_normal.outputs[0], superhero_node.inputs["PrimaryNormal"])
                    if len(superhero_node.inputs["SecondaryNormal"].links) == 0:
                        links.new(base_normal.outputs[0], superhero_node.inputs["SecondaryNormal"])

                    if sticker_texture_data := get_param_info(textures, "Sticker"):
                        if "/Game/Global/Textures/Default/Blanks/" not in sticker_texture_data.get("Texture").get("Path"):
                            sticker_node = nodes.new(type="ShaderNodeTexImage")
                            sticker_node.image = self.import_image(sticker_texture_data.get("Texture").get("Path"))
                            sticker_node.image.alpha_mode = 'CHANNEL_PACKED'
                            sticker_node.image.colorspace_settings.name = "sRGB" if sticker_texture_data.get("Texture").get("sRGB") else "Non-Color"
                            sticker_node.interpolation = "Smart"
                            sticker_node.extension = "CLIP"
                            sticker_node.hide = True
                            sticker_node.location = [-885, -585]
                            
                            back_sticker_node = nodes.new(type="ShaderNodeTexImage")
                            back_sticker_node.image = self.import_image(sticker_texture_data.get("Texture").get("Path"))
                            back_sticker_node.image.alpha_mode = 'CHANNEL_PACKED'
                            back_sticker_node.image.colorspace_settings.name = "sRGB" if sticker_texture_data.get("Texture").get("sRGB") else "Non-Color"
                            back_sticker_node.interpolation = "Smart"
                            back_sticker_node.extension = "CLIP"
                            back_sticker_node.hide = True
                            back_sticker_node.location = [-885, -640]
                        
                            pre_superhero_node = nodes.new(type="ShaderNodeGroup")
                            pre_superhero_node.node_tree = bpy.data.node_groups.get("FPv3 Pre Superhero")
                            pre_superhero_node.location = -1150, -560
                            setup_params(superhero_mappings, pre_superhero_node, False)

                            links.new(pre_superhero_node.outputs["StickerUV"], sticker_node.inputs[0])
                            links.new(pre_superhero_node.outputs["StickerMask"], superhero_node.inputs["StickerMask"])
                            links.new(sticker_node.outputs[0], superhero_node.inputs["Sticker"])
                            links.new(sticker_node.outputs[1], superhero_node.inputs["StickerAlpha"])

                            links.new(pre_superhero_node.outputs["BackStickerUV"], back_sticker_node.inputs[0])
                            links.new(pre_superhero_node.outputs["BackStickerMask"], superhero_node.inputs["BackStickerMask"])
                            links.new(back_sticker_node.outputs[0], superhero_node.inputs["BackSticker"])
                            links.new(back_sticker_node.outputs[1], superhero_node.inputs["BackStickerAlpha"])

                    shader_node.inputs["Background Diffuse Alpha"].default_value = 0.0
                    links.new(superhero_node.outputs["Diffuse"], shader_node.inputs["Background Diffuse"])
                    links.new(superhero_node.outputs["Normals"], shader_node.inputs["Normals"])
                    links.new(superhero_node.outputs["SpecularMasks"], shader_node.inputs["SpecularMasks"])
                    links.new(superhero_node.outputs["ClothFuzzChannel"], shader_node.inputs["Cloth Channel"])

                if get_param(switches, "UseUV2Composite"):
                    composite_node = nodes.new(type="ShaderNodeGroup")
                    composite_node.node_tree = bpy.data.node_groups.get("FPv3 Composite")
                    composite_node.location = -600, 0
                    setup_params(composite_mappings, composite_node, False)

                    pre_composite_node = nodes.new(type="ShaderNodeGroup")
                    pre_composite_node.node_tree = bpy.data.node_groups.get("FPv3 Pre Composite")
                    pre_composite_node.location = -1150, -225
                    setup_params(composite_mappings, pre_composite_node, False)

                    connect_texture_uvs(composite_node, "UV2Composite_AlphaTexture", pre_composite_node.outputs[0])
                    connect_texture_uvs(composite_node, "UV2Composite_Diffuse", pre_composite_node.outputs[1])
                    connect_texture_uvs(composite_node, "UV2Composite_Normals", pre_composite_node.outputs[1])
                    connect_texture_uvs(composite_node, "UV2Composite_SRM", pre_composite_node.outputs[1])

                    move_texture_node(composite_node, "Diffuse")
                    move_texture_node(composite_node, "Normals")
                    move_texture_node(composite_node, "SpecularMasks")

                    if diffuse_node := get_node(composite_node, "Diffuse"):
                        nodes.active = diffuse_node

                if "M_Detail_Texturing_Parent_2025" in base_material_path:
                    detail_node = nodes.new(type="ShaderNodeGroup")
                    detail_node.node_tree = bpy.data.node_groups.get("FPv3 Detail")
                    detail_node.location = -600, 0
                    setup_params(detail_mappings, detail_node, False)

                    pre_detail_node = nodes.new(type="ShaderNodeGroup")
                    pre_detail_node.node_tree = bpy.data.node_groups.get("FPv3 Pre Detail")
                    pre_detail_node.location = -1150, -350
                    setup_params(detail_mappings, pre_detail_node, False)

                    connect_texture_uvs(detail_node, "Detail Diffuse", pre_detail_node.outputs[0])
                    connect_texture_uvs(detail_node, "Detail Normal", pre_detail_node.outputs[0])
                    connect_texture_uvs(detail_node, "Detail SRM", pre_detail_node.outputs[0])

                    move_texture_node(detail_node, "Diffuse")
                    move_texture_node(detail_node, "Normals")
                    move_texture_node(detail_node, "SpecularMasks")

                    if diffuse_node := get_node(detail_node, "Diffuse"):
                        nodes.active = diffuse_node
                    
                if get_param(switches, "UseSequins"):
                    sequin_node = nodes.new(type="ShaderNodeGroup")
                    sequin_node.node_tree = bpy.data.node_groups.get("FPv3 Sequin")
                    sequin_node.location = -600, 0
                    setup_params(sequin_mappings, sequin_node, False)
                    if get_param(textures, "SRM"):
                        set_param("SwizzleRoughnessToGreen", 1, sequin_node)

                    pre_sequin_node = nodes.new(type="ShaderNodeGroup")
                    pre_sequin_node.node_tree = bpy.data.node_groups.get("FPv3 Pre Sequin")
                    pre_sequin_node.location = -1150, -350
                    setup_params(sequin_mappings, pre_sequin_node, False)
                    
                    if "M_DimeBlanket_Parent" not in base_material_path:
                        connect_or_add_default_texture(sequin_node, "SequinOffset", "T_SequinTile.png", False, pre_sequin_node.outputs[0])
                        connect_or_add_default_texture(sequin_node, "SequinRoughness", "T_SequinTile_roughness.png", False, pre_sequin_node.outputs[0])
                        connect_or_add_default_texture(sequin_node, "SequinNormal", "T_SequinTile_N.png", False, pre_sequin_node.outputs[0])
                        
                        if get_param(switches, "MFSequin_UseThinFilmOnSequins"):
                            connect_or_add_default_texture(sequin_node, "SequinThinFilmColor", "T_ThinFilm_Spectrum_COLOR", True, pre_sequin_node.outputs[1])
                    
                    move_texture_node(sequin_node, "Diffuse")
                    move_texture_node(sequin_node, "Normals")
                    move_texture_node(sequin_node, "SpecularMasks")
                    move_texture_node(sequin_node, "Emission")
                    move_texture_node(sequin_node, "SkinFX_Mask")

                    if diffuse_node := get_node(sequin_node, "Diffuse"):
                        nodes.active = diffuse_node
                    
                    if "M_Sequin_Parent_StrideMice" in base_material_path:
                        connect_or_add_default_texture(sequin_node, "StripeMask", "T_SequinTile_StripesMask.png", False, pre_sequin_node.outputs[0])
                        set_param("UseStripes", 1, sequin_node)
                    
                    if "M_DimeBlanket_Parent" in base_material_path:
                        setup_params(sequin_secondary_mappings, sequin_node, False)
                        setup_params(sequin_secondary_mappings, pre_sequin_node, False)
                        connect_or_add_default_texture(sequin_node, "SequinOffset", "T_SequinTile.png", False, pre_sequin_node.outputs[0])
                        connect_or_add_default_texture(sequin_node, "SequinRoughness", "T_SequinTile_roughness.png", False, pre_sequin_node.outputs[0])
                        connect_or_add_default_texture(sequin_node, "SequinNormal", "T_SequinTile_N.png", False, pre_sequin_node.outputs[0])
                        connect_or_add_default_texture(sequin_node, "SequinThinFilmColor", "T_ThinFilm_Spectrum_COLOR", True, pre_sequin_node.outputs[1])
                        
                        trim_sequin_node = nodes.new(type="ShaderNodeGroup")
                        trim_sequin_node.node_tree = bpy.data.node_groups.get("FPv3 Sequin")
                        trim_sequin_node.location = -1650, 0
                        setup_params(sequin_mappings, trim_sequin_node, False)
                        setup_params(sequin_trim_mappings, trim_sequin_node, False)
                        if get_param(textures, "SRM"):
                            set_param("SwizzleRoughnessToGreen", 1, sequin_node)
    
                        pre_trim_sequin_node = nodes.new(type="ShaderNodeGroup")
                        pre_trim_sequin_node.node_tree = bpy.data.node_groups.get("FPv3 Pre Sequin")
                        pre_trim_sequin_node.location = -2200, -350
                        setup_params(sequin_mappings, pre_trim_sequin_node, False)
                        setup_params(sequin_trim_mappings, pre_trim_sequin_node, False)
                        
                        connect_or_add_default_texture(trim_sequin_node, "SequinOffset", "T_SequinTile.png", False, pre_trim_sequin_node.outputs[0])
                        connect_or_add_default_texture(trim_sequin_node, "SequinRoughness", "T_SequinTile_roughness.png", False, pre_trim_sequin_node.outputs[0])
                        connect_or_add_default_texture(trim_sequin_node, "SequinNormal", "T_SequinTile_N.png", False, pre_trim_sequin_node.outputs[0])
                        connect_or_add_default_texture(trim_sequin_node, "SequinThinFilmColor", "T_ThinFilm_Spectrum_COLOR", True, pre_trim_sequin_node.outputs[1])
                    
                        move_texture_node(trim_sequin_node, "Diffuse", sequin_node)
                        move_texture_node(trim_sequin_node, "Normals", sequin_node)
                        move_texture_node(trim_sequin_node, "SpecularMasks", sequin_node)
                        move_texture_node(trim_sequin_node, "Emission", sequin_node)
                        move_texture_node(trim_sequin_node, "SkinFX_Mask", sequin_node)

            case "FPv3 Glass":
                mask_slot = shader_node.inputs["Mask"]
                if len(mask_slot.links) > 0 and get_param(switches, "Use Diffuse Texture for Color [ignores alpha channel]"):
                    links.remove(mask_slot.links[0])

                if color_node := get_node(shader_node, "Color"):
                    nodes.active = color_node
            
            case "FPv3 Bean Costume":
                set_param("Ambient Occlusion", self.options.get("AmbientOcclusion"))
                mask_slot = shader_node.inputs["MaterialMasking"]
                position = get_param(vectors, "Head_Costume_UVPatternPosition" if meta.get("IsHead") else "Costume_UVPatternPosition")
                if position and len(mask_slot.links) > 0:
                    mask_node = mask_slot.links[0].from_node
                    mask_node.extension = "CLIP"

                    mask_position_node = nodes.new("ShaderNodeGroup")
                    mask_position_node.node_tree = bpy.data.node_groups.get("FPv3 Bean Mask Position")
                    mask_position_node.location = mask_node.location + Vector((-200, 25))
                    mask_position_node.inputs["Costume_UVPatternPosition"].default_value = position.get('R'), position.get('G'), position.get('B')
                    links.new(mask_position_node.outputs[0], mask_node.inputs[0])
                
            case "FPv3 Toon":
                set_param("Brightness", self.options.get("ToonShadingBrightness"))
                self.add_toon_outline = True
            
            case "FPv3 3L Eyes":
                pre_eye_node = nodes.new(type="ShaderNodeGroup")
                pre_eye_node.node_tree = bpy.data.node_groups.get("FPv3 Pre 3L Eyes")
                pre_eye_node.location = -600, 0
                setup_params(socket_mappings, pre_eye_node, False)
                
                for texture_mapping in socket_mappings.textures:
                    if node := get_node(shader_node, texture_mapping.slot):
                        links.new(pre_eye_node.outputs["EyeUVs"], node.inputs[0])

                if diffuse_node := get_node(shader_node, "Diffuse"):
                    nodes.active = diffuse_node
            
            case "FPv3 Layer":
                if diffuse_node := get_node(shader_node, "Diffuse"):
                    nodes.active = diffuse_node
                    
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
                self.import_material(mat_mesh.material_slots[material.get("Slot")], material, {})
            else:
                self.import_material(None, material, {}, True)