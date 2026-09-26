import os.path
import bpy


class FontImportContext:
    
    def import_font_data(self, data):
        self.import_font(data.get("Path"))
        
    def import_font(self, path: str):
        path = path[1:] if path.startswith("/") else path
        file_path, name = path.split(".")
        font_path = os.path.join(self.assets_root, file_path + ".ttf")
        bpy.ops.font.open(filepath=font_path, check_existing=True)