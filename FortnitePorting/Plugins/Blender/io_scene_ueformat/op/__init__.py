import bpy
from bpy.props import PointerProperty
from bpy.types import Context, Menu, Scene

from io_scene_ueformat.op.import_helpers import UFImportUEAnim, UFImportUEModel, UFImportUEWorld
from io_scene_ueformat.op.panels import UEFORMAT_PT_Panel
from io_scene_ueformat.op.settings import UFSettings

operators = [UEFORMAT_PT_Panel, UFImportUEModel, UFImportUEAnim, UFImportUEWorld, UFSettings]


def draw_import_menu(self: Menu, context: Context) -> None:  # noqa: ARG001
    self.layout.operator(UFImportUEModel.bl_idname, text="Unreal Model (.uemodel)")
    self.layout.operator(UFImportUEAnim.bl_idname, text="Unreal Animation (.ueanim)")
    self.layout.operator(UFImportUEAnim.bl_idname, text="Unreal World (.ueworld)")


def register() -> None:
    for operator in operators:
        bpy.utils.register_class(operator)

    Scene.uf_settings = PointerProperty(type=UFSettings)  # type: ignore[reportAttributeAccessIssue]
    bpy.types.TOPBAR_MT_file_import.append(draw_import_menu)


def unregister() -> None:
    for operator in operators:
        bpy.utils.unregister_class(operator)

    del Scene.uf_settings  # type: ignore[reportAttributeAccessIssue]
    bpy.types.TOPBAR_MT_file_import.remove(draw_import_menu)
