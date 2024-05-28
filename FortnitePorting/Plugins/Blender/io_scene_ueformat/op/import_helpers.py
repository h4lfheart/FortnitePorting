from pathlib import Path
from typing import Generic, TypeVar

from bpy.props import CollectionProperty, StringProperty
from bpy.types import Operator, OperatorFileListElement
from bpy_extras.io_utils import ImportHelper

from io_scene_ueformat.importer.logic import UEFormatImport
from io_scene_ueformat.op.panels import UEFORMAT_PT_Panel
from io_scene_ueformat.options import UEAnimOptions, UEFormatOptions, UEModelOptions, UEWorldOptions
from io_scene_ueformat.typing import UFormatContext

T = TypeVar("T", bound=UEFormatOptions)


class UFImportBase(Operator, ImportHelper, Generic[T]):
    bl_context = "scene"
    files: CollectionProperty(
        type=OperatorFileListElement,
        options={"HIDDEN", "SKIP_SAVE"},
    )
    directory: StringProperty(subtype="DIR_PATH")

    options_class: type[T]

    def execute(self, context: UFormatContext) -> set[str]:
        options = self.options_class.from_settings(context.scene.uf_settings)

        directory = Path(self.directory)
        for file in self.files:
            file: OperatorFileListElement
            UEFormatImport(options).import_file(directory / file.name)

        return {"FINISHED"}


class UFImportUEModel(UFImportBase):
    bl_idname = "uf.import_uemodel"
    bl_label = "Import Model"

    filename_ext = ".uemodel"
    filter_glob: StringProperty(default="*.uemodel", options={"HIDDEN"}, maxlen=255)

    options_class = UEModelOptions

    def draw(self, context: UFormatContext) -> None:
        UEFORMAT_PT_Panel.draw_general_options(self, context.scene.uf_settings)
        UEFORMAT_PT_Panel.draw_model_options(
            self,
            context.scene.uf_settings,
            import_menu=True,
        )


class UFImportUEAnim(UFImportBase):
    bl_idname = "uf.import_ueanim"
    bl_label = "Import Animation"

    filename_ext = ".ueanim"
    filter_glob: StringProperty(default="*.ueanim", options={"HIDDEN"}, maxlen=255)

    options_class = UEAnimOptions

    def draw(self, context: UFormatContext) -> None:
        UEFORMAT_PT_Panel.draw_general_options(self, context.scene.uf_settings)
        UEFORMAT_PT_Panel.draw_anim_options(
            self,
            context.scene.uf_settings,
            import_menu=True,
        )

class UFImportUEWorld(UFImportBase):
    bl_idname = "uf.import_ueworld"
    bl_label = "Import World"

    filename_ext = ".ueworld"
    filter_glob: StringProperty(default="*.ueworld", options={"HIDDEN"}, maxlen=255)

    options_class = UEWorldOptions

    def draw(self, context: UFormatContext) -> None:
        UEFORMAT_PT_Panel.draw_general_options(self, context.scene.uf_settings)
        UEFORMAT_PT_Panel.draw_world_options(
            self,
            context.scene.uf_settings,
            import_menu=True,
        )
