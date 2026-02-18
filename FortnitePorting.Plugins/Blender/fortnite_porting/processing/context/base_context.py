import bpy
from ..mappings import *
from ..enums import *
from ..utils import *
from ...utils import *
from ...logger import Log

class BaseImportContext:
    
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
        self.override_morph_targets = []
        self.imported_meshes = []
        self.full_vertex_crunch_materials = []
        self.partial_vertex_crunch_materials = {}
        self.add_toon_outline = False

        if bpy.context.mode != "OBJECT":
            bpy.ops.object.mode_set(mode='OBJECT')

        ensure_blend_data()

        import_type = EPrimitiveExportType(data.get("PrimitiveType"))
        
        if import_type == EPrimitiveExportType.MESH:
            self.import_mesh_data(data)
        elif import_type == EPrimitiveExportType.ANIMATION:
            self.import_anim_data(data)
        elif import_type == EPrimitiveExportType.TEXTURE:
            self.import_texture_data(data)
        elif import_type == EPrimitiveExportType.SOUND:
            self.import_sound_data(data)
        elif import_type == EPrimitiveExportType.FONT:
            self.import_font_data(data)
        elif import_type == EPrimitiveExportType.POSE_ASSET:
            self.import_pose_asset_data(data, get_selected_armature(), None)
        elif import_type == EPrimitiveExportType.MATERIAL:
            self.import_material_standalone(data)
        elif import_type == EPrimitiveExportType.TASTY_RIG:
            self.import_tasty_rig_standalone(data)

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