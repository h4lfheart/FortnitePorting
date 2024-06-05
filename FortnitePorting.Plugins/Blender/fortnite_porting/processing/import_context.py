import bpy
import os
from .enums import *
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

        if bpy.context.mode != "OBJECT":
            bpy.ops.object.mode_set(mode='OBJECT')

        import_type = EPrimitiveExportType(data.get("PrimitiveType"))
        match import_type:
            case EPrimitiveExportType.MESH:
                self.import_mesh_data(data)
            case EPrimitiveExportType.ANIMATION:
                pass
            case EPrimitiveExportType.TEXTURE:
                pass
            case EPrimitiveExportType.SOUND:
                pass

    def import_mesh_data(self, data):

        if self.type in [EExportType.OUTFIT, EExportType.BACKPACK]:
            target_meshes = data.get("OverrideMeshes")
            normal_meshes = data.get("Meshes")
            for mesh in normal_meshes:
                if not any(target_meshes, lambda target_mesh: target_mesh.get("Type") == mesh.get("Type")):
                    target_meshes.append(mesh)
        else:
            target_meshes = data.get("Meshes")

        self.meshes = target_meshes
        for mesh in target_meshes:
            self.import_mesh(mesh.get("Path"))

    def import_mesh(self, path: str):
        options = UEModelOptions(scale_factor=0.01 if self.options.get("ScaleDown") else 1,
                                 reorient_bones=self.options.get("ReorientBones"),
                                 bone_length=self.options.get("BoneLength"))

        path = path[1:] if path.startswith("/") else path

        mesh_path = os.path.join(self.assets_root, path.split(".")[0] + ".uemodel")

        return UEFormatImport(options).import_file(mesh_path)
