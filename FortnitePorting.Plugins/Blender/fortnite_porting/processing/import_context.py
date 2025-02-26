import json
import traceback
import pyperclip

import bpy
import traceback
from math import radians
from .mappings import *
from .material import *
from .enums import *
from .utils import *
from .tasty import *
from ..utils import *
from ..logger import Log
from ...io_scene_ueformat.importer.logic import UEFormatImport
from ...io_scene_ueformat.options import UEModelOptions, UEAnimOptions

class ImportContext:

    def __init__(self, meta_data):
        self.options = meta_data.get("Settings")
        self.assets_root = meta_data.get("AssetsRoot")

    def run(self, data):
        self.name = data.get("Name")
        self.type = EExportType(data.get("Type"))
        self.scale = 0.01 if self.options.get("ScaleDown") else 1
        self.meshes = []
        self.override_materials = []
        self.override_parameters = []
        self.imported_meshes = []
        self.full_vertex_crunch_materials = []
        self.partial_vertex_crunch_materials = {}
        self.add_toon_outline = False

        if bpy.context.mode != "OBJECT":
            bpy.ops.object.mode_set(mode='OBJECT')

        ensure_blend_data()

        #pyperclip.copy(json.dumps(data))

        import_type = EPrimitiveExportType(data.get("PrimitiveType"))
        match import_type:
            case EPrimitiveExportType.MESH:
                self.import_mesh_data(data)
            case EPrimitiveExportType.ANIMATION:
                self.import_anim_data(data)
                pass
            case EPrimitiveExportType.TEXTURE:
                self.import_texture_data(data)
                pass
            case EPrimitiveExportType.SOUND:
                self.import_sound_data(data)
                pass
            case EPrimitiveExportType.FONT:
                self.import_font_data(data)
                pass
            case EPrimitiveExportType.POSE_ASSET:
                self.import_pose_asset_data(data, get_selected_armature(), None)
                pass
            case EPrimitiveExportType.MATERIAL:
                self.import_material_standalone(data)
                pass

    def import_mesh_data(self, data):
        rig_type = ERigType(self.options.get("RigType"))
        
        if rig_type == ERigType.TASTY:
            self.options["MergeArmatures"] = True
            self.options["ReorientBones"] = True
        
        self.override_materials = data.get("OverrideMaterials")
        self.override_parameters = data.get("OverrideParameters")
        
        self.collection = create_or_get_collection(self.name) if self.options.get("ImportIntoCollection") else bpy.context.scene.collection

        if self.type in [EExportType.OUTFIT, EExportType.BACKPACK, EExportType.PICKAXE, EExportType.FALL_GUYS_OUTFIT]:
            target_meshes = data.get("OverrideMeshes")
            normal_meshes = data.get("Meshes")
            for mesh in normal_meshes:
                if not any(target_meshes, lambda target_mesh: target_mesh.get("Type") == mesh.get("Type")):
                    target_meshes.append(mesh)
        else:
            target_meshes = data.get("Meshes")

        self.meshes = target_meshes
        for mesh in target_meshes:
            self.import_model(mesh, can_spawn_at_3d_cursor=True)

        self.import_light_data(data.get("Lights"))
            
        if self.type in [EExportType.OUTFIT, EExportType.FALL_GUYS_OUTFIT] and self.options.get("MergeArmatures"):
            master_skeleton = merge_armatures(self.imported_meshes)
            master_mesh = get_armature_mesh(master_skeleton)
            
            for material, elements in self.partial_vertex_crunch_materials.items():
                vertex_crunch_modifier = master_mesh.modifiers.new("FPv3 Vertex Crunch", type="NODES")
                vertex_crunch_modifier.node_group = bpy.data.node_groups.get("FPv3 Vertex Crunch")

                set_geo_nodes_param(vertex_crunch_modifier, "Material", material)
                for name, value in elements.items():
                    set_geo_nodes_param(vertex_crunch_modifier, name, value == 1)
                    
            for material in self.full_vertex_crunch_materials:
                vertex_crunch_modifier = master_mesh.modifiers.new("FPv3 Full Vertex Crunch", type="NODES")
                vertex_crunch_modifier.node_group = bpy.data.node_groups.get("FPv3 Full Vertex Crunch")
                set_geo_nodes_param(vertex_crunch_modifier, "Material", material)

            if self.add_toon_outline:
                master_mesh.data.materials.append(bpy.data.materials.get("M_FP_Outline"))

                solidify = master_mesh.modifiers.new(name="Outline", type='SOLIDIFY')
                solidify.thickness = 0.001
                solidify.offset = 1
                solidify.thickness_clamp = 5.0
                solidify.use_rim = False
                solidify.use_flip_normals = True
                solidify.material_offset = len(master_mesh.data.materials) - 1
                
            if rig_type == ERigType.TASTY:
                create_tasty_rig(self, master_skeleton, TastyRigOptions(scale=self.scale, use_dynamic_bone_shape=self.options.get("UseDynamicBoneShape")))

            if anim_data := data.get("Animation"):
                self.import_anim_data(anim_data, master_skeleton)

    def gather_metadata(self, *search_props):
        out_props = {}
        for mesh in self.meshes:
            meta = mesh.get("Meta")
            if meta is None:
                continue

            for search_prop in search_props:
                if found_key := first(meta.keys(), lambda key: key == search_prop):
                    if out_props.get(found_key):
                        if meta.get(found_key):
                            Log.warn(f"{found_key}: metadata already set "
                                     "with content from different mesh but "
                                     f"also found on {mesh.get('Name')} "
                                     "which will be ignored")
                        continue
                    out_props[found_key] = meta.get(found_key)
        return out_props

    def get_metadata(self, search_prop):
        for mesh in self.meshes:
            meta = mesh.get("Meta")
            if meta is None:
                continue

            if found_key := first(meta.keys(), lambda key: key == search_prop):
                return meta.get(found_key)
        return None

    def import_model(self, mesh, parent=None, can_reorient=True, can_spawn_at_3d_cursor=False):
        path = mesh.get("Path")
        name = mesh.get("Name")
        part_type = EFortCustomPartType(mesh.get("Type"))
        num_lods = mesh.get("NumLods")
        
        if mesh.get("IsEmpty"):
            empty_object = bpy.data.objects.new(name, None)

            empty_object.parent = parent
            empty_object.rotation_euler = make_euler(mesh.get("Rotation"))
            empty_object.location = make_vector(mesh.get("Location"), unreal_coords_correction=True) * self.scale
            empty_object.scale = make_vector(mesh.get("Scale"))
            
            self.collection.objects.link(empty_object)
            
            for child in mesh.get("Children"):
                self.import_model(child, parent=empty_object)
                
            return
        
        if self.type in [EExportType.PREFAB, EExportType.WORLD] and mesh in self.meshes:
            Log.info(f"Importing Actor: {name} {self.meshes.index(mesh)} / {len(self.meshes)}")

        mesh_name = path.split(".")[1]
        if self.type in [EExportType.PREFAB, EExportType.WORLD] and (existing_mesh_data := bpy.data.meshes.get(mesh_name + "_LOD0")):
            imported_object = bpy.data.objects.new(name, existing_mesh_data)
            self.collection.objects.link(imported_object)
            
            imported_mesh = get_armature_mesh(imported_object)
        else:
            imported_object = self.import_mesh(path, can_reorient=can_reorient)
            if imported_object is None:
                Log.warn(f"Import failed for object at path: {path}")
                return imported_object
            imported_object.name = name

            imported_mesh = get_armature_mesh(imported_object)

            if EPolygonType(self.options.get("PolygonType")) == EPolygonType.QUADS and imported_mesh is not None:
                bpy.context.view_layer.objects.active = imported_mesh
                bpy.ops.object.mode_set(mode='EDIT')
                bpy.ops.mesh.tris_convert_to_quads(uvs=True)
                bpy.ops.object.mode_set(mode='OBJECT')
                bpy.context.view_layer.objects.active = imported_object

        if (override_vertex_colors := mesh.get("OverrideVertexColors")) and len(override_vertex_colors) > 0:
            imported_mesh.data = imported_mesh.data.copy()

            vertex_color = imported_mesh.data.color_attributes.new(
                domain="CORNER",
                type="BYTE_COLOR",
                name="INSTCOL0",
            )

            color_data = []
            for col in override_vertex_colors:
                color_data.append((col["R"], col["G"], col["B"], col["A"]))

            for polygon in imported_mesh.data.polygons:
                for vertex_index, loop_index in zip(polygon.vertices, polygon.loop_indices):
                    if vertex_index >= len(color_data):
                        continue
                        
                    color = color_data[vertex_index]
                    vertex_color.data[loop_index].color = color[0] / 255, color[1] / 255, color[2] / 255, color[3] / 255

        imported_object.parent = parent
        imported_object.rotation_euler = make_euler(mesh.get("Rotation"))
        imported_object.location = make_vector(mesh.get("Location"), unreal_coords_correction=True) * self.scale
        imported_object.scale = make_vector(mesh.get("Scale"))
        
        if self.options.get("ImportAt3DCursor") and can_spawn_at_3d_cursor:
            imported_object.location += bpy.context.scene.cursor.location

        self.imported_meshes.append({
            "Skeleton": imported_object,
            "Mesh": imported_mesh,
            "Type": part_type,
            "Meta": mesh.get("Meta")
        })

        # metadata handling
        # todo extract meta reading to function bc this is too big
        meta = self.gather_metadata("PoseData", "CurveTrackNames")

        # pose asset
        if imported_mesh is not None:
            bpy.context.view_layer.objects.active = imported_mesh
            self.import_pose_asset_data(meta, get_selected_armature(), part_type)

        # end

        match part_type:
            case EFortCustomPartType.BODY:
                meta.update(self.gather_metadata("SkinColor"))
            case EFortCustomPartType.HEAD:
                meta.update(self.gather_metadata("MorphNames", "HatType"))
                meta["IsHead"] = True
                shape_keys = imported_mesh.data.shape_keys
                if (morphs := meta.get("MorphNames")) and (morph_name := morphs.get(meta.get("HatType"))) and shape_keys is not None:
                    for key in shape_keys.key_blocks:
                        if key.name.casefold() == morph_name.casefold():
                            key.value = 1.0

        meta["TextureData"] = mesh.get("TextureData")
        
        for material in mesh.get("Materials"):
            index = material.get("Slot")
            if index >= len(imported_mesh.material_slots):
                continue

            self.import_material(imported_mesh.material_slots[index], material, meta)

        for override_material in mesh.get("OverrideMaterials"):
            index = override_material.get("Slot")
            if index >= len(imported_mesh.material_slots):
                continue

            overridden_material = imported_mesh.material_slots[index]
            slots = where(imported_mesh.material_slots,
                          lambda slot: slot.name == overridden_material.name)
            for slot in slots:
                self.import_material(slot, override_material, meta)

        for variant_override_material in self.override_materials:
            material_name_to_swap = variant_override_material.get("MaterialNameToSwap")
            
            slots = where(imported_mesh.material_slots,
                          lambda slot: slot.material.get("OriginalName") == material_name_to_swap)
            for slot in slots:
                self.import_material(slot, variant_override_material.get("Material"), meta)
                
        self.import_light_data(mesh.get("Lights"), imported_object)

        for child in mesh.get("Children"):
            self.import_model(child, parent=imported_object)
            
        instances = mesh.get("Instances")
        if len(instances) > 0:
            mesh_data = imported_mesh.data
            imported_object.select_set(True)
            bpy.ops.object.delete()
            
            instance_parent = bpy.data.objects.new("InstanceParent_" + name, None)
            instance_parent.parent = parent
            instance_parent.rotation_euler = make_euler(mesh.get("Rotation"))
            instance_parent.location = make_vector(mesh.get("Location"), unreal_coords_correction=True) * self.scale
            instance_parent.scale = make_vector(mesh.get("Scale"))
            bpy.context.collection.objects.link(instance_parent)
            
            for instance_index, instance_transform in enumerate(instances):
                instance_name = f"Instance_{instance_index}_" + name
                
                Log.info(f"Importing Instance: {instance_name} {instance_index} / {len(instances)}")
                
                instance_object = bpy.data.objects.new(f"Instance_{instance_index}_" + name, mesh_data)
                self.collection.objects.link(instance_object)
    
                instance_object.parent = instance_parent
                instance_object.rotation_euler = make_euler(instance_transform.get("Rotation"))
                instance_object.location = make_vector(instance_transform.get("Location"), unreal_coords_correction=True) * self.scale
                instance_object.scale = make_vector(instance_transform.get("Scale"))
            
        return imported_object
    
    def import_light_data(self, lights, parent=None):
        if not lights:
            return
        
        for point_light in lights.get("PointLights"):
            self.import_point_light(point_light, parent)
    
    def import_point_light(self, point_light, parent=None):
        name = point_light.get("Name")
        light_data = bpy.data.lights.new(name=name, type='POINT')
        light = bpy.data.objects.new(name=name, object_data=light_data)
        self.collection.objects.link(light)
        
        light.parent = parent
        light.rotation_euler = make_euler(point_light.get("Rotation"))
        light.location = make_vector(point_light.get("Location"), unreal_coords_correction=True) * self.scale
        light.scale = make_vector(point_light.get("Scale"))
        
        color = point_light.get("Color")
        light_data.color = (color["R"], color["G"], color["B"])
        light_data.energy = point_light.get("Intensity")
        light_data.use_custom_distance = True
        light_data.cutoff_distance = point_light.get("AttenuationRadius") * self.scale
        light_data.shadow_soft_size = point_light.get("Radius") * self.scale
        light_data.use_shadow = point_light.get("CastShadows")

    def import_mesh(self, path: str, can_reorient=True):
        options = UEModelOptions(scale_factor=self.scale,
                                 reorient_bones=self.options.get("ReorientBones") and can_reorient,
                                 bone_length=self.options.get("BoneLength"),
                                 import_sockets=self.options.get("ImportSockets"),
                                 import_virtual_bones=self.options.get("ImportVirtualBones"),
                                 import_collision=self.options.get("ImportCollision"),
                                 target_lod=self.options.get("TargetLOD"),
                                 allowed_reorient_children=allowed_reorient_children)

        path = path[1:] if path.startswith("/") else path

        mesh_path = os.path.join(self.assets_root, path.split(".")[0] + ".uemodel")

        return UEFormatImport(options).import_file(mesh_path)
    
    def import_texture_data(self, data):
        import_method = ETextureImportMethod(self.options.get("TextureImportMethod"))

        for path in data.get("Textures"):
            image = self.import_image(path)
            
            if import_method == ETextureImportMethod.OBJECT:
                bpy.ops.mesh.primitive_plane_add()
                plane = bpy.context.active_object
                plane.name = image.name
                plane.scale.x = image.size[0] / image.size[1]
    
                material = bpy.data.materials.new(image.name)
                material.use_nodes = True
                material.surface_render_method = "BLENDED"
                
                nodes = material.node_tree.nodes
                nodes.clear()
                links = material.node_tree.links
                links.clear()
                
                output_node = nodes.new(type="ShaderNodeOutputMaterial")
                output_node.location = (300, 0)
                
                image_node = nodes.new(type="ShaderNodeTexImage")
                image_node.image = image
                links.new(image_node.outputs[0], output_node.inputs[0])
                
                plane.data.materials.append(material)

    def format_image_path(self, path: str):
        path, name = path.split(".")
        path = path[1:] if path.startswith("/") else path
        
        ext = ""
        match EImageFormat(self.options.get("ImageFormat")):
            case EImageFormat.PNG:
                ext = "png"
            case EImageFormat.TGA:
                ext = "tga"
        
        texture_path = os.path.join(self.assets_root, path + "." + ext)
        return texture_path, name

    def import_image(self, path: str):
        path, name = self.format_image_path(path)
        if existing := bpy.data.images.get(name):
            return existing

        if not os.path.exists(path):
            return None

        return bpy.data.images.load(path, check_existing=True)

    def import_material(self, material_slot, material_data, meta, as_material_data=False):

        # object ref mat slots for instancing
        if not as_material_data:
            temp_material = material_slot.material
            material_slot.link = 'OBJECT' if self.type in [EExportType.WORLD, EExportType.PREFAB] else 'DATA'
            material_slot.material = temp_material

        material_name = material_data.get("Name")
        material_hash = material_data.get("Hash")
        additional_hash = 0

        texture_data = meta.get("TextureData")
        if texture_data is not None:
            for data in texture_data:
                additional_hash += data.get("Hash")
        
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

        if texture_data is not None:
            for data in texture_data:
                replace_or_add_parameter(textures, data.get("Diffuse"))
                replace_or_add_parameter(textures, data.get("Normal"))
                replace_or_add_parameter(textures, data.get("Specular"))

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

        unused_parameter_height = 0

        # parameter handlers
        def texture_param(data, target_mappings, target_node=shader_node, add_unused_params=False):
            try:
                name = data.get("Name")
                path = data.get("Value")
                texture_name = path.split(".")[1]

                node = nodes.new(type="ShaderNodeTexImage")
                node.image = self.import_image(path)
                node.image.alpha_mode = 'CHANNEL_PACKED'
                node.image.colorspace_settings.name = "sRGB" if data.get("sRGB") else "Non-Color"
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
                target_node.inputs[mappings.slot].default_value = (value["R"], value["G"], value["B"], 1.0)
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

        if "M_Eyes_Parent" in base_material_path or get_param(scalars, "Eye Cornea IOR") is not None:
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

        match shader_node.node_tree.name:
            case "FPv3 Material":
                # PRE FX START
                pre_fx_node = nodes.new(type="ShaderNodeGroup")
                pre_fx_node.node_tree = bpy.data.node_groups.get("FPv3 Pre FX")
                pre_fx_node.location = -600, -700
                setup_params(socket_mappings, pre_fx_node, False)

                thin_film_node = get_node(shader_node, "Thin Film Texture")
                if get_param_multiple(switches, ["Use Thin Film", "UseThinFilm"]) or "M_ReconExpert_FNCS_Parent" in base_material_path:
                    if thin_film_node is None:
                        thin_film_node = nodes.new(type="ShaderNodeTexImage")
                        thin_film_node.image = bpy.data.images.get("T_ThinFilm_Spectrum_COLOR")
                        thin_film_node.image.alpha_mode = 'CHANNEL_PACKED'
                        thin_film_node.image.colorspace_settings.name = "sRGB"
                        thin_film_node.interpolation = "Smart"
                        thin_film_node.hide = True
                        
                        x, y = get_socket_pos(shader_node, shader_node.inputs.find("Thin Film Texture"))
                        thin_film_node.location = x - 300, y
                        links.new(thin_film_node.outputs[0], shader_node.inputs["Thin Film Texture"])
                        
                    links.new(pre_fx_node.outputs["Thin Film UV"], thin_film_node.inputs[0])
                elif thin_film_node is not None:
                    nodes.remove(thin_film_node)

                cloth_fuzz_node = get_node(shader_node, "ClothFuzz Texture")
                if get_param_multiple(switches, ["Use Cloth Fuzz", "UseClothFuzz"]):
                    if cloth_fuzz_node is None:
                        cloth_fuzz_node = nodes.new(type="ShaderNodeTexImage")
                        cloth_fuzz_node.image = bpy.data.images.get("T_Fuzz_MASK")
                        cloth_fuzz_node.image.alpha_mode = 'CHANNEL_PACKED'
                        cloth_fuzz_node.image.colorspace_settings.name = "sRGB"
                        cloth_fuzz_node.interpolation = "Smart"
                        cloth_fuzz_node.hide = True

                        x, y = get_socket_pos(shader_node, shader_node.inputs.find("ClothFuzz Texture"))
                        cloth_fuzz_node.location = x - 300, y
                        links.new(cloth_fuzz_node.outputs[0], shader_node.inputs["ClothFuzz Texture"])

                    links.new(pre_fx_node.outputs["Cloth UV"], cloth_fuzz_node.inputs[0])
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

                if get_param(switches, "Use Vertex Colors for Mask"):
                    elements = {}
                    for scalar in scalars:
                        name = scalar.get("Name")
                        if "Hide Element" not in name:
                            continue

                        elements[name] = scalar.get("Value")
                    
                    self.partial_vertex_crunch_materials[material] = elements

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
                    gmap_node.node_tree = bpy.data.node_groups.get(".FP GMap Material")
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
                    gmap_node.node_tree = bpy.data.node_groups.get(".FP GMap")
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
                    eye_texture_node.image = self.import_image(eye_texture_data.get("Value"))
                    eye_texture_node.image.alpha_mode = 'CHANNEL_PACKED'
                    eye_texture_node.image.colorspace_settings.name = "sRGB" if eye_texture_data.get("sRGB") else "Non-Color"
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
                        if "/Game/Global/Textures/Default/Blanks/" not in sticker_texture_data.get("Value"):
                            sticker_node = nodes.new(type="ShaderNodeTexImage")
                            sticker_node.image = self.import_image(sticker_texture_data.get("Value"))
                            sticker_node.image.alpha_mode = 'CHANNEL_PACKED'
                            sticker_node.image.colorspace_settings.name = "sRGB" if sticker_texture_data.get("sRGB") else "Non-Color"
                            sticker_node.interpolation = "Smart"
                            sticker_node.extension = "CLIP"
                            sticker_node.hide = True
                            sticker_node.location = [-885, -585]
                            
                            back_sticker_node = nodes.new(type="ShaderNodeTexImage")
                            back_sticker_node.image = self.import_image(sticker_texture_data.get("Value"))
                            back_sticker_node.image.alpha_mode = 'CHANNEL_PACKED'
                            back_sticker_node.image.colorspace_settings.name = "sRGB" if sticker_texture_data.get("sRGB") else "Non-Color"
                            back_sticker_node.interpolation = "Smart"
                            back_sticker_node.extension = "CLIP"
                            back_sticker_node.hide = True
                            back_sticker_node.location = [-885, -640]
                        
                            pre_superhero_node = nodes.new(type="ShaderNodeGroup")
                            pre_superhero_node.node_tree = bpy.data.node_groups.get("FPv3 Pre Superhero")
                            pre_superhero_node.location = -1150, -560
                            pre_superhero_node.inputs["StickerPosition"].default_value = get_vector_param(vectors, "StickerPosition")
                            pre_superhero_node.inputs["StickerScale"].default_value = get_vector_param(vectors, "StickerScale")
                            pre_superhero_node.inputs["BackStickerPosition"].default_value = get_vector_param(vectors, "BackStickerPosition")
                            pre_superhero_node.inputs["BackStickerScale"].default_value = get_vector_param(vectors, "BackStickerScale")

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

    def import_sound_data(self, data):
        for sound in data.get("Sounds"):
            path = sound.get("Path")
            self.import_sound(path, time_to_frame(sound.get("Time")))

    def import_sound(self, path: str, time):
        path = path[1:] if path.startswith("/") else path
        file_path, name = path.split(".")
        if existing := bpy.data.sounds.get(name):
            return existing

        if not bpy.context.scene.sequence_editor:
            bpy.context.scene.sequence_editor_create()

        ext = ESoundFormat(self.options.get("SoundFormat")).name.lower()
        sound_path = os.path.join(self.assets_root, f"{file_path}.{ext}")
        sound = bpy.context.scene.sequence_editor.sequences.new_sound(name, sound_path, 0, time)
        sound["FPSound"] = True
        return sound
    
    def import_anim_data(self, data, override_skeleton=None):
        target_skeleton = override_skeleton or get_selected_armature()
        bpy.context.view_layer.objects.active = target_skeleton
        
        if target_skeleton is None:
            # TODO message server
            #MessageServer.instance.send("An armature must be selected to import an animation. Please select an armature and try again.")
            return

        if target_skeleton.get("is_tasty"):
            bpy.ops.object.mode_set(mode="POSE")
            
            if ik_finger_toggle := target_skeleton.pose.bones.get("finger_toggle"):
                ik_finger_toggle.location[0] = TOGGLE_OFF

            if world_space_pole_toggle := target_skeleton.pose.bones.get("pole_toggle"):
                world_space_pole_toggle.location[0] = TOGGLE_OFF
                
            bpy.ops.object.mode_set(mode="OBJECT")
            

        # clear old data
        if anim_data := target_skeleton.animation_data:
           anim_data.action = None
           
           for track in anim_data.nla_tracks:
               anim_data.nla_tracks.remove(track)
        else:
            target_skeleton.animation_data_create()

        active_mesh = get_armature_mesh(target_skeleton)
        if active_mesh is not None and active_mesh.data.shape_keys is not None:
            active_mesh.data.shape_keys.name = "Pose Asset Controls"
            
            if shape_key_anim_data := active_mesh.data.shape_keys.animation_data:
                shape_key_anim_data.action = None
                for track in shape_key_anim_data.nla_tracks:
                    shape_key_anim_data.nla_tracks.remove(track)
            else:
                active_mesh.data.shape_keys.animation_data_create()
            
        if bpy.context.scene.sequence_editor:
            sequences_to_remove = where(bpy.context.scene.sequence_editor.sequences, lambda seq: seq.get("FPSound"))
            for sequence in sequences_to_remove:
                bpy.context.scene.sequence_editor.sequences.remove(sequence)

        bpy.context.scene.frame_set(0)

        # start import
        target_track = target_skeleton.animation_data.nla_tracks.new(prev=None)
        target_track.name = "Sections"

        if active_mesh.data.shape_keys is not None:
            mesh_track = active_mesh.data.shape_keys.animation_data.nla_tracks.new(prev=None)
            mesh_track.name = "Sections"

        def import_sections(sections, skeleton, track, is_main_skeleton = False):
            total_frames = 0
            is_metahuman = any(skeleton.data.bones, lambda bone: bone.name == "FACIAL_C_FacialRoot")
            for section in sections:
                path = section.get("Path")

                total_frames += time_to_frame(section.get("Length"))

                anim = self.import_anim(path, skeleton)
                clear_children_bone_transforms(skeleton, anim, "faceAttach")

                section_name = section.get("Name")
                time_offset = section.get("Time")
                loop_count = 999 if self.options.get("LoopAnimation") and section.get("Loop") else 1
                frame = time_to_frame(time_offset)

                if len(track.strips) > 0 and frame < track.strips[-1].frame_end:
                    frame = int(track.strips[-1].frame_end)

                strip = track.strips.new(section_name, frame, anim)
                strip.repeat = loop_count

                if (curves := section.get("Curves")) and len(curves) > 0 and active_mesh.data.shape_keys is not None and is_main_skeleton:
                    key_blocks = active_mesh.data.shape_keys.key_blocks
                    for key_block in key_blocks:
                        key_block.value = 0

                    for curve in curves:
                        curve_name = curve.get("Name")
                        if target_block := key_blocks.get(curve_name.replace("CTRL_expressions_", "")):
                            for key in curve.get("Keys"):
                                target_block.value = key.get("Value")
                                target_block.keyframe_insert(data_path="value", frame=key.get("Time") * 30)

                        if is_metahuman and (curve_mappings := metahuman_mappings.get(curve_name)):
                            for curve_mapping in curve_mappings:
                                if target_block := key_blocks.get(curve_mapping.replace("CTRL_expressions_", "")):
                                    for key in curve.get("Keys"):
                                        target_block.value = key.get("Value")
                                        target_block.keyframe_insert(data_path="value", frame=key.get("Time") * 30)

                    if active_mesh.data.shape_keys.animation_data.action is not None:
                        strip = mesh_track.strips.new(section_name, frame, active_mesh.data.shape_keys.animation_data.action)
                        strip.name = section_name
                        strip.repeat = loop_count
                        active_mesh.data.shape_keys.animation_data.action = None
            return total_frames

        total_frames = import_sections(data.get("Sections"), target_skeleton, target_track, True)
        if self.options.get("UpdateTimelineLength"):
            bpy.context.scene.frame_end = total_frames

        props = data.get("Props")
        if len(props) > 0:
            if master_skeleton := first(target_skeleton.children, lambda child: child.name == "Master_Skeleton"):
                bpy.data.objects.remove(master_skeleton)

            master_skeleton = self.import_model(data.get("Skeleton"), can_reorient=False)
            master_skeleton.name = "Master_Skeleton"
            master_skeleton.parent = target_skeleton
            master_skeleton.animation_data_create()

            master_track = master_skeleton.animation_data.nla_tracks.new(prev=None)
            master_track.name = "Sections"

            import_sections(data.get("Sections"), master_skeleton, master_track)

            for prop in props:
                mesh = self.import_model(prop.get("Mesh"))
                constraint_object(mesh, master_skeleton, prop.get("SocketName"), [0, 0, 0])
                mesh.rotation_euler = make_euler(prop.get("RotationOffset"))
                mesh.location = make_vector(prop.get("LocationOffset"), unreal_coords_correction=True) * 0.01
                mesh.scale = make_vector(prop.get("Scale"))

                if (anims := prop.get("AnimSections")) and len(anims) > 0:
                    mesh.animation_data_create()
                    mesh_track = mesh.animation_data.nla_tracks.new(prev=None)
                    mesh_track.name = "Sections"
                    import_sections(anims, mesh, mesh_track)

            master_skeleton.hide_set(True)

        if self.options.get("ImportSounds"):
            for sound in data.get("Sounds"):
                path = sound.get("Path")
                self.import_sound(path, time_to_frame(sound.get("Time")))

    def import_anim(self, path: str, override_skeleton=None):
        path = path[1:] if path.startswith("/") else path
        file_path, name = path.split(".")
        if (existing := bpy.data.actions.get(name)) and existing["Skeleton"] == override_skeleton.name:
            return existing

        anim_path = os.path.join(self.assets_root, file_path + ".ueanim")
        options = UEAnimOptions(link=False,
                                override_skeleton=override_skeleton,
                                scale_factor=self.scale)
        anim = UEFormatImport(options).import_file(anim_path)
        anim["Skeleton"] = override_skeleton.name
        return anim
    
    def import_font_data(self, data):
        self.import_font(data.get("Path"))
        
    def import_font(self, path: str):
        path = path[1:] if path.startswith("/") else path
        file_path, name = path.split(".")
        font_path = os.path.join(self.assets_root, file_path + ".ttf")
        bpy.ops.font.open(filepath=font_path, check_existing=True)
        
    def import_pose_asset_data(self, data, selected_armature, part_type):
        pose_data = data.get("PoseData")
        if not pose_data:
            return

        tracks = data.get("CurveTrackNames")

        original_selected_object = bpy.context.active_object
        if selected_armature is None:
            return

        selected_mesh: bpy.types.Object = get_armature_mesh(selected_armature)

        shape_keys = selected_mesh.data.shape_keys
        original_shape_key_lock = selected_mesh.show_only_shape_key
        original_mode = bpy.context.active_object.mode
        muted_constraints = []
        try:
            bpy.ops.object.mode_set(mode="OBJECT")
            armature_modifier: bpy.types.ArmatureModifier = first(
                selected_mesh.modifiers, lambda mod: mod.type == "ARMATURE"
            )

            # Turn off shape key lock if it's on otherwise shape keys fail to
            # import.
            selected_mesh.show_only_shape_key = False

            # Temporarily mutate all bone constraints since pose assets
            # are bone based and we don't want constraints to influence
            # the final pose.
            muted_constraints = disable_constraints(selected_armature)

            # Swap parents where applicable
            bone_swap_orig_parents(selected_armature)

            if not shape_keys:
                # Create Basis shape key
                selected_mesh.shape_key_add(name="Basis", from_mix=False)

            root_bone_name = "neck_01"
            root_bone = get_case_insensitive(
                selected_armature.pose.bones, root_bone_name
            )
            if not root_bone:
                Log.warn(
                    f"{selected_mesh.name}: Failed to find root bone "
                    f"'{root_bone_name}' in '{selected_armature.name}', all "
                    "bones will be considered during import of "
                    "PoseData"
                )

            loc_scale = self.scale
            pose_names = []
            for pose in pose_data:
                if not (pose_name := pose.get("Name")):
                    Log.warn(
                        f"{selected_mesh.name}: skipping pose data "
                        f"with no pose name:\n{pose}"
                    )
                    continue
                pose_names.append(pose_name)

                # If there are no influences, don't bother
                if not (influences := pose.get("Keys")):
                    continue

                # Enter pose mode
                bpy.context.view_layer.objects.active = selected_armature
                bpy.ops.object.mode_set(mode="POSE")

                # Reset all transforms to default
                bpy.ops.pose.select_all(action="SELECT")
                bpy.ops.pose.transforms_clear()
                bpy.ops.pose.select_all(action="DESELECT")

                # Move bones accordingly
                contributed = False
                for bone in influences:
                    if not (bone_name := bone.get("Name")):
                        Log.warn(
                            f"{selected_mesh.name} - {pose_name}: "
                            f"empty bone name for pose:\n{pose}"
                        )
                        continue

                    pose_bone: bpy.types.PoseBone = get_case_insensitive(
                        selected_armature.pose.bones, bone_name
                    )
                    if not pose_bone:
                        # For cases where pose data tries to move a non-existent bone
                        # i.e. Poseidon has no 'Tongue' but it's in the pose asset
                        if not part_type or part_type is EFortCustomPartType.HEAD:
                            # There are likely many missing bones in non-Head parts, but we
                            # process as many as we can.
                            Log.warn(
                                f"{selected_mesh.name} - {pose_name}: "
                                f"'{bone_name}' influence skipped "
                                "since it was not found in "
                                f"'{selected_armature.name}'"
                            )
                        continue

                    if root_bone and not bone_has_parent(pose_bone, root_bone):
                        Log.warn(
                            f"{selected_mesh.name} - {pose_name}: "
                            f"skipped '{pose_bone.name}' since it does "
                            f"not have '{root_bone.name}' as a parent"
                        )
                        continue

                    # Verify that the current bone and all of its children
                    # have at least one vertex group associated with it
                    if not bone_hierarchy_has_vertex_groups(
                        pose_bone, selected_mesh.vertex_groups
                    ):
                        continue

                    # Reset bone to identity
                    pose_bone.matrix_basis.identity()

                    rotation = bone.get("Rotation")
                    if not rotation.get("IsNormalized"):
                        Log.warn(
                            f"{selected_mesh.name} - {pose_name}: "
                            f"rotation not normalized for '{bone_name}' in "
                            "pose"
                        )

                    edit_bone = pose_bone.bone
                    post_quat = (
                        Quaternion(post_quat)
                        if (post_quat := edit_bone.get("post_quat"))
                        else Quaternion()
                    )

                    q = post_quat.copy()
                    q.rotate(make_quat(rotation))
                    quat = post_quat.copy()
                    quat.rotate(q.conjugated())
                    pose_bone.rotation_quaternion = (
                        quat.conjugated() @ pose_bone.rotation_quaternion
                    )

                    loc = make_vector(
                        bone.get("Location"), unreal_coords_correction=True
                    )
                    loc.rotate(post_quat.conjugated())

                    pose_bone.location = pose_bone.location + loc * loc_scale
                    pose_bone.scale = Vector((1, 1, 1)) + make_vector(bone.get("Scale"))

                    pose_bone.rotation_quaternion.normalize()
                    contributed = True

                # Do not create shape keys if nothing changed
                if not contributed:
                    continue

                # Create blendshape from armature
                bpy.ops.object.mode_set(mode="OBJECT")
                bpy.context.view_layer.objects.active = selected_mesh
                selected_mesh.select_set(True)
                bpy.ops.object.modifier_apply_as_shapekey(
                    keep_modifier=True, modifier=armature_modifier.name
                )

                # Use name from pose data
                selected_mesh.data.shape_keys.key_blocks[-1].name = pose_name

            if not tracks:
                return

            bpy.ops.object.mode_set(mode="OBJECT")
            bpy.context.view_layer.objects.active = selected_mesh
            selected_mesh.select_set(True)

            # Now that base blendshapes are imported, cycle through all
            # PoseData again to create blendshapes based on CurveData.
            for pose in pose_data:
                if not (curves := pose.get("CurveData")):
                    continue

                if not (pose_name := pose.get("Name")):
                    Log.warn(
                        f"{selected_mesh.name} - {pose_name}: skipping pose "
                        f"data from curve data with no pose name:\n{pose}"
                    )
                    continue

                # Not sure what it means when there's curve data on a pose
                # also containing bone transforms. So if there's an existing
                # shape key, just prepend curves_ to distinguish it.
                # Also, if it exists in the original set of shape keys but it
                # failed to import for some reason (i.e. missing bone), also
                # distinguish that name to prevent confusion since the name
                # of that key may not do what the user expects
                # (i.e. tongue_up_pose on Fish Thicc created from CurveData).
                # If we import outside the context of a full character import
                # (i.e. part_type is None), then only prepend curves_ for
                # existing shape keys.
                if pose_name in selected_mesh.data.shape_keys.key_blocks or \
                   (part_type is EFortCustomPartType.HEAD and pose_name in pose_names):
                    pose_name = f"curves_{pose_name}"

                # Verify length of CurveData matches that of tracks
                if len(curves) != len(tracks):
                    Log.warn(
                        f"{selected_mesh.name} - {pose_name}: skipped since "
                        "length of curve data for pose does not match the "
                        "length of Tracks array"
                    )
                    continue

                # If all curve values are basically 0, skip
                if all(curves, lambda curve_value: abs(curve_value) < 0.00001):
                    continue

                contributed = False
                for track_idx, curve_value in enumerate(curves):
                    # Sometimes curve_value is a very small number (1.7881586e-06).
                    # Probably best to just normalize it to 0
                    if abs(curve_value) < 0.00001:
                        curve_value = 0.0

                    # Even if the curve_value is zero, let it be set anyway
                    # since this is essentially a free reset to zero for the
                    # relevant shape keys.
                    shape_key_name = tracks[track_idx]
                    if shape_key := selected_mesh.data.shape_keys.key_blocks.get(
                        shape_key_name
                    ):
                        # Influence above / below 1.0 is possible with curve data
                        if curve_value < shape_key.slider_min:
                            shape_key.slider_min = curve_value - 1.0
                        if curve_value > shape_key.slider_max:
                            shape_key.slider_max = curve_value + 1.0
                        shape_key.value = curve_value
                        contributed = True
                    else:
                        if not part_type or part_type is EFortCustomPartType.HEAD:
                            Log.warn(
                                f"{selected_mesh.name} - {pose_name}: did not "
                                "apply influence to missing shape key: "
                                f"'{shape_key_name}'"
                            )

                # Do not create shape keys if nothing changed
                if not contributed:
                    continue

                selected_mesh.shape_key_add(name=pose_name, from_mix=True)

            # Set all shape keys in the track back to 0
            for shape_key_name in tracks:
                if shape_key := selected_mesh.data.shape_keys.key_blocks.get(
                    shape_key_name
                ):
                    shape_key.slider_min = 0.0
                    shape_key.slider_max = 1.0
                    shape_key.value = 0.0
        except Exception as e:
            Log.error(f"Failed to import PoseAsset data from {selected_mesh.name}: {e}")
        finally:
            # Final reset before re-entering regular import mode.
            bpy.context.view_layer.objects.active = selected_armature
            bpy.ops.object.mode_set(mode="POSE")
            bpy.ops.pose.select_all(action="SELECT")
            bpy.ops.pose.transforms_clear()
            bpy.ops.pose.select_all(action="DESELECT")

            bone_swap_orig_parents(selected_armature)
            for constraint in muted_constraints:
                constraint.mute = False

            selected_mesh.show_only_shape_key = original_shape_key_lock
            bpy.ops.object.mode_set(mode=original_mode)
            bpy.context.view_layer.objects.active = original_selected_object

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