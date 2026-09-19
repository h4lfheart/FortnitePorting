import bpy
import json
from bpy.utils import register_class, unregister_class
from ..server import Server


class IMPORT_OT_fp_drag_drop(bpy.types.Operator):
    bl_idname = "import.fp_drag_drop"
    bl_label = "Import Asset"
    bl_options = {'UNDO'}

    filepath: bpy.props.StringProperty(
        subtype='FILE_PATH',
        options={'SKIP_SAVE'}
    )

    def execute(self, context):
        try:
            with open(self.filepath, 'r') as f:
                data = json.load(f)

            asset_paths = data.get('Paths', [])
            if not asset_paths:
                return {'CANCELLED'}

            if Server.instance is None:
                return {'CANCELLED'}

            Server.instance.send_drag_drop_request({'paths': asset_paths})
            return {'FINISHED'}

        except Exception as e:
            self.report({'ERROR'}, f"Error: {str(e)}")
            return {'CANCELLED'}


class IMPORT_FH_fp_drag_drop(bpy.types.FileHandler):
    bl_idname = "IMPORT_FH_fp_drag_drop"
    bl_label = "Asset Drag Drop"
    bl_import_operator = "import.fp_drag_drop"
    bl_file_extensions = ".fp_drag_drop"

    @classmethod
    def poll_drop(cls, context):
        return context.area is not None


def register():
    register_class(IMPORT_OT_fp_drag_drop)
    register_class(IMPORT_FH_fp_drag_drop)


def unregister():
    unregister_class(IMPORT_FH_fp_drag_drop)
    unregister_class(IMPORT_OT_fp_drag_drop)