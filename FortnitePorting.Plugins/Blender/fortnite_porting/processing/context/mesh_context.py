import os.path
import bpy
from math import radians

from ..mappings import *
from ..enums import *
from ..utils import *
from ...utils import *
from ...logger import Log
from ...ueformat.importer.logic import UEFormatImport
from ...ueformat.options import UEModelOptions

class MeshImportContext:
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

        if self.type in [EExportType.SIDEKICK]:
            master_mesh = self.imported_meshes[0]["Mesh"]
            for material in self.full_vertex_crunch_materials:
                vertex_crunch_modifier = master_mesh.modifiers.new("FPv4 Full Vertex Crunch", type="NODES")
                vertex_crunch_modifier.node_group = bpy.data.node_groups.get("FPv4 Full Vertex Crunch")
                set_geo_nodes_param(vertex_crunch_modifier, "Material", material)
                
        if self.type in [EExportType.OUTFIT]:
            for imported_mesh in self.imported_meshes:
                self.parent_deform_bones(imported_mesh["Skeleton"], ["dfrm_", "deform_"])
            
        if self.type in [EExportType.OUTFIT, EExportType.FALL_GUYS_OUTFIT] and self.options.get("MergeArmatures"):
            master_skeleton = merge_armatures(self.imported_meshes)
            master_mesh = get_armature_mesh(master_skeleton)
            # Update attribute to account for joined mesh
            self.update_preskinned_bounds(master_mesh)
            
            for material, elements in self.partial_vertex_crunch_materials.items():
                vertex_crunch_modifier = master_mesh.modifiers.new("FPv4 Vertex Crunch", type="NODES")
                vertex_crunch_modifier.node_group = bpy.data.node_groups.get("FPv4 Vertex Crunch")

                set_geo_nodes_param(vertex_crunch_modifier, "Material", material)
                for name, value in elements.items():
                    set_geo_nodes_param(vertex_crunch_modifier, name, value == 1)
                    
            for material in self.full_vertex_crunch_materials:
                vertex_crunch_modifier = master_mesh.modifiers.new("FPv4 Full Vertex Crunch", type="NODES")
                vertex_crunch_modifier.node_group = bpy.data.node_groups.get("FPv4 Full Vertex Crunch")
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
                self.create_tasty_rig(master_skeleton, self.get_metadata("MasterSkeletalMesh"))

            if anim_data := data.get("Animation"):
                self.import_anim_data(anim_data, master_skeleton)

    def import_model(self, mesh, parent=None, can_reorient=True, can_spawn_at_3d_cursor=False):
        path = mesh.get("Path")
        name = mesh.get("Name")
        part_type = EFortCustomPartType(mesh.get("Type"))
        
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

        # Only add preskinned attributes if they don't already exist
        if imported_mesh is not None and imported_mesh.data.attributes.get("PS_LOCAL_POSITION") is None:
            preskinned_pos = imported_mesh.data.attributes.new( domain="POINT", type="FLOAT_VECTOR",  name="PS_LOCAL_POSITION")
            preskinned_normal = imported_mesh.data.attributes.new(domain="POINT", type="FLOAT_VECTOR", name="PS_LOCAL_NORMAL")

            for vert in imported_mesh.data.vertices:
                preskinned_pos.data[vert.index].vector = vert.co
                preskinned_normal.data[vert.index].vector = vert.normal

            self.update_preskinned_bounds(imported_mesh, True)

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
        meta = self.gather_metadata("PoseAsset")

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
                
        for texture_data in mesh.get("TextureData"):
            if not (td_override_material := texture_data.get("OverrideMaterial")):
                continue
                
            Log.info(f"TextureData Override {td_override_material.get('Path')}")

            index = td_override_material.get("Slot")
            if index >= len(imported_mesh.material_slots):
                continue

            overridden_material = imported_mesh.material_slots[index]
            slots = where(imported_mesh.material_slots,
                          lambda slot: slot.name == overridden_material.name)
            for slot in slots:
                self.import_material(slot, td_override_material, meta)
                
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
    

    def update_preskinned_bounds(self, imported_mesh, new_attribute=False):
        corners = imported_mesh.bound_box
        x_coords, y_coords, z_coords = zip(*corners)
        bbox_min = (min(x_coords), min(y_coords), min(z_coords))
        bbox_max = (max(x_coords), max(y_coords), max(z_coords))

        def map_bounds(point):
            x_range = bbox_max[0] - bbox_min[0]
            y_range = bbox_max[1] - bbox_min[1]
            z_range = bbox_max[2] - bbox_min[2]
    
            x_mapped = (point[0] - bbox_min[0]) / x_range if x_range != 0 else 0.0
            y_mapped = (point[1] - bbox_min[1]) / y_range if y_range != 0 else 0.0
            z_mapped = (point[2] - bbox_min[2]) / z_range if z_range != 0 else 0.0
            
            return x_mapped, y_mapped, z_mapped

        if new_attribute:
            preskinned_bounds = imported_mesh.data.attributes.new(domain="POINT", type="FLOAT_VECTOR", name="PS_LOCAL_BOUNDS")
        else:
            preskinned_bounds = imported_mesh.data.attributes.get("PS_LOCAL_BOUNDS")

        for vert in imported_mesh.data.vertices:
            preskinned_bounds.data[vert.index].vector = map_bounds(vert.co)
            
    def parent_deform_bones(self, skeleton, prefixes):
        bpy.context.view_layer.objects.active = skeleton
        bpy.ops.object.mode_set(mode='EDIT')
    
        edit_bones = skeleton.data.edit_bones
    
        for bone in edit_bones:
            for prefix in prefixes:
                if bone.name.startswith(prefix):
                    parent_name = bone.name[len(prefix):]
                    parent_bone = edit_bones.get(parent_name)
    
                    if parent_bone:
                        bone.parent = parent_bone
                        bone.use_connect = False
                    break
    
        bpy.ops.object.mode_set(mode='OBJECT')
    
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

        mesh, mesh_data = UEFormatImport(options).import_file(mesh_path)
        return mesh