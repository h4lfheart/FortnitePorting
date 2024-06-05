from bpy.types import Context, Scene

from .op.settings import UFSettings


class UFormatScene(Scene):
    uf_settings: UFSettings


class UFormatContext(Context):
    scene: UFormatScene
