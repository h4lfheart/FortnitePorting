
import os.path
from ...ueformat.importer.logic import UEFormatImport
from ...ueformat.options import UEPoseOptions

class PoseImportContext:

    def import_pose_asset_data(self, data, selected_armature, part_type):
        if pose_path := data.get("PoseAsset"):
            self.import_pose_asset(pose_path, selected_armature)
    
    def import_pose_asset(self, path: str, override_skeleton=None):
        path = path[1:] if path.startswith("/") else path
        file_path, name = path.split(".")
        pose_path = os.path.join(self.assets_root, file_path + ".uepose")

        options = UEPoseOptions(scale_factor=self.scale,
                                override_skeleton=override_skeleton,
                                root_bone="neck_01")

        UEFormatImport(options).import_file(pose_path)