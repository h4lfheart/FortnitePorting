import json
import traceback
import pyperclip

import bpy

from .mappings import *
from .material import *
from .enums import *
from .utils import *
from ..utils import *
from ..logger import Log
from ...io_scene_ueformat.importer.logic import UEFormatImport
from ...io_scene_ueformat.options import UEModelOptions, UEAnimOptions

SCALE_FACTOR = 0.01


class ImportContext:

    def __init__(self, meta_data):
        self.options = meta_data.get("Settings")
        self.assets_root = meta_data.get("AssetsRoot")

    def run(self, data):
        self.name = data.get("Name")
        self.type = EExportType(data.get("Type"))
        self.meshes = []
        self.override_materials = []
        self.override_parameters = []
        self.imported_meshes = []

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

    def import_mesh_data(self, data):
        self.override_materials = data.get("OverrideMaterials")
        self.override_parameters = data.get("OverrideParameters")
        
        self.collection = create_or_get_collection(self.name) if self.options.get("ImportIntoCollection") else bpy.context.scene.collection

        if self.type in [EExportType.OUTFIT, EExportType.BACKPACK, EExportType.FALL_GUYS_OUTFIT]:
            target_meshes = data.get("OverrideMeshes")
            normal_meshes = data.get("Meshes")
            for mesh in normal_meshes:
                if not any(target_meshes, lambda target_mesh: target_mesh.get("Type") == mesh.get("Type")):
                    target_meshes.append(mesh)
        else:
            target_meshes = data.get("Meshes")

        self.meshes = target_meshes
        for mesh in target_meshes:
            self.import_model(mesh)
            
        if self.type in [EExportType.OUTFIT, EExportType.FALL_GUYS_OUTFIT] and self.options.get("MergeArmatures"):
            master_skeleton = merge_armatures(self.imported_meshes)
            master_mesh = get_armature_mesh(master_skeleton)

    def import_model(self, mesh, parent=None):
        path = mesh.get("Path")
        name = mesh.get("Name")
        part_type = EFortCustomPartType(mesh.get("Type"))
        num_lods = mesh.get("NumLods")
        
        if mesh.get("IsEmpty"):
            empty_object = bpy.data.objects.new(name, None)

            empty_object.parent = parent
            empty_object.rotation_euler = make_euler(mesh.get("Rotation"))
            empty_object.location = make_vector(mesh.get("Location"), unreal_coords_correction=True) * SCALE_FACTOR
            empty_object.scale = make_vector(mesh.get("Scale"))
            
            self.collection.objects.link(empty_object)
            
            for child in mesh.get("Children"):
                self.import_model(child, parent=empty_object)
                
            return

        mesh_name = path.split(".")[1]
        if self.type in [EExportType.PREFAB, EExportType.WORLD] and (existing_mesh_data := bpy.data.meshes.get(mesh_name + "_LOD0")):
            imported_object = bpy.data.objects.new(name, existing_mesh_data)
            self.collection.objects.link(imported_object)
        else:
            imported_object = self.import_mesh(path)
            imported_object.name = name

        imported_object.parent = parent
        imported_object.rotation_euler = make_euler(mesh.get("Rotation"))
        imported_object.location = make_vector(mesh.get("Location"), unreal_coords_correction=True) * SCALE_FACTOR
        imported_object.scale = make_vector(mesh.get("Scale"))

        imported_mesh = get_armature_mesh(imported_object)
        
        self.imported_meshes.append({
            "Skeleton": imported_object,
            "Mesh": imported_mesh,
            "Type": part_type,
            "Meta": mesh.get("Meta")
        })
        
        

        # metadata handling
        def gather_metadata(*search_props):
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

        # todo extract meta reading to function bc this is too big
        meta = gather_metadata("PoseData")

        # pose asset
        if imported_mesh is not None and (pose_data := meta.get("PoseData")):
            shape_keys = imported_mesh.data.shape_keys
            armature = imported_object
            original_mode = bpy.context.active_object.mode
            try:
                bpy.ops.object.mode_set(mode='OBJECT')
                armature_modifier: bpy.types.ArmatureModifier = first(imported_mesh.modifiers, lambda mod: mod.type == "ARMATURE")

                if not shape_keys:
                    # Create Basis shape key
                    imported_mesh.shape_key_add(name="Basis", from_mix=False)

                root_bone_name = 'neck_01'
                root_bone = get_case_insensitive(armature.pose.bones, root_bone_name)
                if not root_bone:
                    Log.warn(f"{imported_mesh.name}: Failed to find root bone "
                             f"'{root_bone_name}' in '{armature.name}', all "
                             "bones will be considered during import of "
                             "PoseData")

                loc_scale = (SCALE_FACTOR if self.options.get("ScaleDown") else 1)
                for pose in pose_data:
                    # If there are no influences, don't bother
                    if not (influences := pose.get('Keys')):
                        continue

                    if not (pose_name := pose.get('Name')):
                        Log.warn(f"{imported_mesh.name}: skipping pose data "
                                 f"with no pose name: {pose}")
                        continue

                    # Enter pose mode
                    bpy.context.view_layer.objects.active = armature
                    bpy.ops.object.mode_set(mode='POSE')

                    # Reset all transforms to default
                    bpy.ops.pose.select_all(action='SELECT')
                    bpy.ops.pose.transforms_clear()
                    bpy.ops.pose.select_all(action='DESELECT')

                    # Move bones accordingly
                    contributed = False
                    for bone in influences:
                        if not (bone_name := bone.get('Name')):
                            Log.warn(f"{imported_mesh.name} - {pose_name}: "
                                     f"empty bone name for pose '{pose}'")
                            continue

                        pose_bone: bpy.types.PoseBone = get_case_insensitive(armature.pose.bones, bone_name)
                        if not pose_bone:
                            # For cases where pose data tries to move a non-existent bone
                            # i.e. Poseidon has no 'Tongue' but it's in the pose asset
                            if part_type is EFortCustomPartType.HEAD:
                                # There are likely many missing bones in non-Head parts, but we
                                # process as many as we can.
                                Log.warn(f"{imported_mesh.name} - {pose_name}: "
                                         f"'{bone_name}' influence skipped "
                                         "since it was not found in "
                                         f"'{armature.name}'")
                            continue

                        if root_bone and not bone_has_parent(pose_bone, root_bone):
                            Log.warn(f"{imported_mesh.name} - {pose_name}: "
                                     f"skipped '{pose_bone.name}' since it does "
                                     f"not have '{root_bone.name}' as a parent")
                            continue

                        # Verify that the current bone and all of its children
                        # have at least one vertex group associated with it
                        if not bone_hierarchy_has_vertex_groups(pose_bone, imported_mesh.vertex_groups):
                            continue

                        # Reset bone to identity
                        pose_bone.matrix_basis.identity()

                        rotation = bone.get('Rotation')
                        if not rotation.get('IsNormalized'):
                            Log.warn(f"rotation not normalized for {bone_name} in pose {pose_name}")

                        edit_bone = pose_bone.bone
                        post_quat = Quaternion(post_quat) if (post_quat := edit_bone.get("post_quat")) else Quaternion()

                        q = post_quat.copy()
                        q.rotate(make_quat(rotation))
                        quat = post_quat.copy()
                        quat.rotate(q.conjugated())
                        pose_bone.rotation_quaternion = quat.conjugated() @ pose_bone.rotation_quaternion

                        loc = (make_vector(bone.get('Location'), unreal_coords_correction=True))
                        loc.rotate(post_quat.conjugated())

                        pose_bone.location = pose_bone.location + loc * loc_scale
                        pose_bone.scale = (Vector((1, 1, 1)) + make_vector(bone.get('Scale')))

                        pose_bone.rotation_quaternion.normalize()
                        contributed = True

                    # Do not create shape keys if nothing changed
                    if not contributed:
                        continue

                    # Create blendshape from armature
                    bpy.ops.object.mode_set(mode='OBJECT')
                    bpy.context.view_layer.objects.active = imported_mesh
                    imported_mesh.select_set(True)
                    bpy.ops.object.modifier_apply_as_shapekey(keep_modifier=True,
                                                              modifier=armature_modifier.name)

                    # Use name from pose data
                    imported_mesh.data.shape_keys.key_blocks[-1].name = pose_name
            except Exception as e:
                Log.error("Failed to import PoseAsset data from "
                          f"{imported_mesh.name}: {e}")
            finally:
                # Final reset before re-entering regular import mode.
                bpy.context.view_layer.objects.active = armature
                bpy.ops.object.mode_set(mode='POSE')
                bpy.ops.pose.select_all(action='SELECT')
                bpy.ops.pose.transforms_clear()
                bpy.ops.pose.select_all(action='DESELECT')
                bpy.ops.object.mode_set(mode=original_mode)
                
        # end

        match part_type:
            case EFortCustomPartType.BODY:
                meta.update(gather_metadata("SkinColor"))
            case EFortCustomPartType.HEAD:
                meta.update(gather_metadata("MorphNames", "HatType"))
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

        for child in mesh.get("Children"):
            self.import_model(child, parent=imported_object)
            
        instances = mesh.get("Instances")
        if len(instances) > 0:
            mesh_data = imported_mesh.data.copy()
            imported_object.select_set(True)
            bpy.ops.object.delete()
            
            instance_parent = bpy.data.objects.new("InstanceParent_" + name, None)
            instance_parent.rotation_euler = make_euler(mesh.get("Rotation"))
            instance_parent.location = make_vector(mesh.get("Location"), unreal_coords_correction=True) * SCALE_FACTOR
            instance_parent.scale = make_vector(mesh.get("Scale"))
            instance_parent.parent = parent
            bpy.context.collection.objects.link(instance_parent)
            
            for instance_index, instance_transform in enumerate(instances):
                instance_object = bpy.data.objects.new(f"Instance_{instance_index}_" + name, mesh_data)
                self.collection.objects.link(instance_object)
    
                instance_object.parent = instance_parent
                instance_object.rotation_euler = make_euler(instance_transform.get("Rotation"))
                instance_object.location = make_vector(instance_transform.get("Location"), unreal_coords_correction=True) * SCALE_FACTOR
                instance_object.scale = make_vector(instance_transform.get("Scale"))
            
        return imported_object

    def import_mesh(self, path: str):
        options = UEModelOptions(scale_factor=0.01 if self.options.get("ScaleDown") else 1,
                                 reorient_bones=self.options.get("ReorientBones"),
                                 bone_length=self.options.get("BoneLength"),
                                 import_sockets=self.options.get("ImportSockets"),
                                 import_virtual_bones=self.options.get("ImportVirtualBones"),
                                 import_collision=self.options.get("ImportCollision"),
                                 target_lod=self.options.get("TargetLOD"))

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

    def import_material(self, material_slot, material_data, meta):

        # object ref mat slots for instancing
        temp_material = material_slot.material
        material_slot.link = 'OBJECT' if self.type in [EExportType.WORLD, EExportType.PREFAB] else 'DATA'
        material_slot.material = temp_material

        material_name = material_data.get("Name")
        material_hash = material_data.get("Hash")
        additional_hash = 0

        texture_data = meta.get("TextureData")
        for data in texture_data:
            additional_hash += data.get("Hash")
        
        override_parameters = where(self.override_parameters, lambda param: param.get("MaterialNameToAlter") in [material_name, "Global"])
        for parameters in override_parameters:
            additional_hash += parameters.get("Hash")

        if additional_hash != 0:
            material_hash += additional_hash
            material_name += f"_{hash_code(material_hash)}"
            
        if existing_material := first(bpy.data.materials, lambda mat: mat.get("Hash") == hash_code(material_hash)):
            material_slot.material = existing_material
            return

        # same name but different hash
        if (name_existing := first(bpy.data.materials, lambda mat: mat.name == material_name)) and name_existing.get("Hash") != material_hash:
            material_name += f"_{hash_code(material_hash)}"
            
        if material_slot.material.name.casefold() != material_name.casefold():
            material_slot.material = bpy.data.materials.new(material_name)

        material_slot.material["Hash"] = hash_code(material_hash)
        material_slot.material["OriginalName"] = material_data.get("Name")

        material = material_slot.material
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

        for data in texture_data:
            replace_or_add_parameter(textures, data.get("Diffuse"))
            replace_or_add_parameter(textures, data.get("Normal"))
            replace_or_add_parameter(textures, data.get("Specular"))

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
        shader_node.node_tree = bpy.data.node_groups.get("FP Material")

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
                target_node.inputs[mappings.slot].default_value = value
                if mappings.switch_slot:
                    target_node.inputs[mappings.switch_slot].default_value = 1 if value else 0
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
                        node.node_tree = bpy.data.node_groups.get("FP Switch")
                        node.inputs[0].default_value = 1 if value else 0
                        node.label = name
                        node.width = 250
                        node.location = 400, unused_parameter_height
                        unused_parameter_height -= 125
                    return

                value = mappings.value_func(value) if mappings.value_func else value
                target_node.inputs[mappings.slot].default_value = 1 if value else 0
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

        if get_param_multiple(switches, layer_switch_names) and get_param_multiple(textures, extra_layer_names):
            replace_shader_node("FP Layer")
            socket_mappings = layer_mappings

            set_param("Is Transparent", override_blend_mode is not EBlendMode.BLEND_Opaque)

        if get_param_multiple(textures, toon_texture_names) or get_param_multiple(vectors, toon_vector_names):
            replace_shader_node("FP Toon")
            socket_mappings = toon_mappings

        if "M_FN_Valet_Master" in material_data.get("BaseMaterialPath"):
            replace_shader_node("FP Valet")
            socket_mappings = valet_mappings

        is_glass = base_blend_mode is EBlendMode.BLEND_Translucent and translucency_lighting_mode in [ETranslucencyLightingMode.TLM_SurfacePerPixelLighting, ETranslucencyLightingMode.TLM_VolumetricPerVertexDirectional]
        if is_glass:
            replace_shader_node("FP Glass")
            socket_mappings = glass_mappings

            material.surface_render_method = "BLENDED"
            material.show_transparent_back = False

        is_trunk = get_param(switches, "IsTrunk")
        if is_trunk:
            socket_mappings = trunk_mappings

        is_foliage = base_blend_mode is EBlendMode.BLEND_Masked and shading_model in [EMaterialShadingModel.MSM_TwoSidedFoliage, EMaterialShadingModel.MSM_Subsurface]
        if is_foliage and not is_trunk:
            replace_shader_node("FP Foliage")
            socket_mappings = foliage_mappings

        if "MM_BeanCharacter_Body" in material_data.get("BaseMaterialPath"):
            replace_shader_node("FP Bean Base")
            socket_mappings = bean_base_mappings
            
        if "MM_BeanCharacter_Costume" in material_data.get("BaseMaterialPath"):
            replace_shader_node("FP Bean Costume")
            socket_mappings = bean_head_costume_mappings if meta.get("IsHead") else bean_costume_mappings

        setup_params(socket_mappings, shader_node, True)

        links.new(shader_node.outputs[0], output_node.inputs[0])

        # post parameter handling

        match shader_node.node_tree.name:
            case "FP Material":
                set_param("AO", self.options.get("AmbientOcclusion"))
                set_param("Cavity", self.options.get("Cavity"))
                set_param("Subsurface", self.options.get("Subsurface"))

                if (skin_color := meta.get("SkinColor")) and skin_color["A"] != 0:
                    set_param("Skin Color", (skin_color["R"], skin_color["G"], skin_color["B"], 1.0))
                    set_param("Skin Boost", skin_color["A"])

                if get_param_multiple(switches, emissive_toggle_names) is False:
                    set_param("Emission Strength", 0)

                if get_param(textures, "SRM"):
                    set_param("SwizzleRoughnessToGreen", 1)

                if get_param(switches, "Use Vertex Colors for Mask"):  # TODO covnert geo nodes
                    color_node = nodes.new(type="ShaderNodeVertexColor")
                    color_node.location = [-400, -560]
                    color_node.layer_name = "COL0"

                    mask_node = nodes.new("ShaderNodeGroup")
                    mask_node.node_tree = bpy.data.node_groups.get("FP Vertex Alpha")
                    mask_node.location = [-200, -560]

                    links.new(color_node.outputs[0], mask_node.inputs[0])
                    links.new(mask_node.outputs[0], shader_node.inputs["Alpha"])

                    for scalar in scalars:
                        name = scalar.get("Name")
                        value = scalar.get("Value")
                        if "Hide Element" not in name:
                            continue

                        if input := mask_node.inputs.get(name.replace("Hide ", "")):
                            input.default_value = int(value)

                emission_slot = shader_node.inputs["Emission"]
                if (crop_bounds := get_param_multiple(vectors, emissive_crop_vector_names)) and get_param_multiple(switches, emissive_crop_switch_names) and len(emission_slot.links) > 0:
                    emission_node = emission_slot.links[0].from_node
                    emission_node.extension = "CLIP"
    
                    crop_texture_node = nodes.new("ShaderNodeGroup")
                    crop_texture_node.node_tree = bpy.data.node_groups.get("FP Texture Cropping")
                    crop_texture_node.location = emission_node.location + Vector((-200, 25))
                    crop_texture_node.inputs["Left"].default_value = crop_bounds.get('R')
                    crop_texture_node.inputs["Top"].default_value = crop_bounds.get('G')
                    crop_texture_node.inputs["Right"].default_value = crop_bounds.get('B')
                    crop_texture_node.inputs["Bottom"].default_value = crop_bounds.get('A')
                    links.new(crop_texture_node.outputs[0], emission_node.inputs[0])

                if get_param(switches, "Modulate Emissive with Diffuse"):
                    diffuse_node = shader_node.inputs["Diffuse"].links[0].from_node
                    links.new(diffuse_node.outputs[0], shader_node.inputs["Emission Multiplier"])

                if get_param(switches, "useGmapGradientLayers"):
                    gradient_node = nodes.new(type="ShaderNodeGroup")
                    gradient_node.node_tree = bpy.data.node_groups.get("FP Gradient")
                    gradient_node.location = -500, 0
                    nodes.remove(shader_node.inputs["Diffuse"].links[0].from_node)
                    links.new(gradient_node.outputs[0], shader_node.inputs[0])

                    gmap_node = nodes.new("ShaderNodeValue")
                    gmap_node.location = -1000, -120
                    gmap_node.outputs[0].default_value = 1

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

            case "FP Glass":
                mask_slot = shader_node.inputs["Mask"]
                if len(mask_slot.links) > 0 and get_param(switches, "Use Diffuse Texture for Color [ignores alpha channel]"):
                    links.remove(mask_slot.links[0])
            
            case "FP Bean Costume":
                set_param("Ambient Occlusion", self.options.get("AmbientOcclusion"))
                mask_slot = shader_node.inputs["MaterialMasking"]
                position = get_param(vectors, "Head_Costume_UVPatternPosition" if meta.get("IsHead") else "Costume_UVPatternPosition")
                if position and len(mask_slot.links) > 0:
                    mask_node = mask_slot.links[0].from_node
                    mask_node.extension = "CLIP"

                    mask_position_node = nodes.new("ShaderNodeGroup")
                    mask_position_node.node_tree = bpy.data.node_groups.get("FP Bean Mask Position")
                    mask_position_node.location = mask_node.location + Vector((-200, 25))
                    mask_position_node.inputs["Costume_UVPatternPosition"].default_value = position.get('R'), position.get('G'), position.get('B')
                    links.new(mask_position_node.outputs[0], mask_node.inputs[0])

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

        sound_path = os.path.join(self.assets_root, file_path + ".wav")  # TODO mp3 import as well bc there's an option
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

        if target_skeleton.get("is_tasty_rig"):
            # TODO make obsolete with tasty rig v3
            #MessageServer.instance.send("Tasty Rig currently does not support emotes. Please use a character with the Default Rig and try again.")
            return

        # clear old data
        target_skeleton.animation_data_clear()
        if bpy.context.scene.sequence_editor:
            sequences_to_remove = where(bpy.context.scene.sequence_editor.sequences, lambda seq: seq.get("FPSound"))
            for sequence in sequences_to_remove:
                bpy.context.scene.sequence_editor.sequences.remove(sequence)

        # start import
        target_skeleton.animation_data_create()
        target_track = target_skeleton.animation_data.nla_tracks.new(prev=None)
        target_track.name = "Sections"

        active_mesh = get_armature_mesh(target_skeleton)
        if active_mesh.data.shape_keys is not None:
            active_mesh.data.shape_keys.name = "Pose Asset Controls"
            active_mesh.data.shape_keys.animation_data_create()
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
                strip = track.strips.new(section_name, time_to_frame(time_offset), anim)
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
                        strip = mesh_track.strips.new(section_name, time_to_frame(time_offset), active_mesh.data.shape_keys.animation_data.action)
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

            master_skeleton = self.import_model(data.get("Skeleton"))
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
                                scale_factor=0.01 if self.options.get("ScaleDown") else 1)
        anim = UEFormatImport(options).import_file(anim_path)
        anim["Skeleton"] = override_skeleton.name
        return anim