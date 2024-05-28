from bpy.types import Context, Scene

from io_scene_ueformat.op.settings import UFSettings


class UFormatScene(Scene):
    uf_settings: UFSettings


class UFormatContext(Context):
    scene: UFormatScene
