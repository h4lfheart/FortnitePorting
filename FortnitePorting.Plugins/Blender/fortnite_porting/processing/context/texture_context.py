import bpy
from ..enums import *
import os.path

class TextureImportContext:
    
    def import_texture_data(self, data):
        import_method = ETextureImportMethod(self.options.get("TextureImportMethod"))

        for texture in data.get("Textures"):
            image = self.import_image(texture.get("Path"))
            
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
                
        hdr_path = os.path.join(self.assets_root, path + ".hdr")
        if os.path.exists(hdr_path):
            return hdr_path, name

        texture_path = os.path.join(self.assets_root, path + "." + ext)
        return texture_path, name

    def import_image(self, path: str):
        path, name = self.format_image_path(path)
        if existing := bpy.data.images.get(name):
            return existing

        if not os.path.exists(path):
            return None

        return bpy.data.images.load(path, check_existing=True)