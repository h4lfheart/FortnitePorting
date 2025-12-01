from pathlib import Path
from typing import Generic, TypeVar

from bpy.props import CollectionProperty, StringProperty
from bpy.types import Operator, OperatorFileListElement, FileHandler
from bpy_extras.io_utils import ImportHelper, poll_file_object_drop

from .panels import UEFORMAT_PT_Panel
from ..importer.logic import UEFormatImport
from ..options import UEAnimOptions, UEFormatOptions, UEModelOptions, UEPoseOptions
from ..typing import UFormatContext

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

    def invoke(self, context, event):
        return ImportHelper.invoke_popup(self, context)

class UFImportUEModel(UFImportBase):
    bl_idname = "uf.import_uemodel"
    bl_label = "Import Model"
    bl_description = "Import Model"

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
    bl_description = "Import Animation"

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

class UFImportUEPose(UFImportBase):
    bl_idname = "uf.import_uepose"
    bl_label = "Import Pose"
    bl_description = "Import Pose"

    filename_ext = ".uepose"
    filter_glob: StringProperty(default="*.uepose", options={"HIDDEN"}, maxlen=255)

    options_class = UEPoseOptions

    def draw(self, context: UFormatContext) -> None:
        UEFORMAT_PT_Panel.draw_general_options(self, context.scene.uf_settings)
        UEFORMAT_PT_Panel.draw_pose_options(
            self,
            context.scene.uf_settings,
            import_menu=True,
        )


# drag and drop handler
class IO_FH_ueformatBase(FileHandler):
    @classmethod
    def poll_drop(cls, context):
        return poll_file_object_drop(context)

class IO_FH_uemodel(IO_FH_ueformatBase):
    bl_idname = "IO_FH_uemodel"
    bl_label = "UEFormat Model"
    bl_import_operator = UFImportUEModel.bl_idname
    bl_file_extensions = ".uemodel"

class IO_FH_ueanim(IO_FH_ueformatBase):
    bl_idname = "IO_FH_ueanim"
    bl_label = "UEFormat Animation"
    bl_import_operator = UFImportUEAnim.bl_idname
    bl_file_extensions = ".ueanim"

class IO_FH_uepose(IO_FH_ueformatBase):
    bl_idname = "IO_FH_uepose"
    bl_label = "UEFormat Pose"
    bl_import_operator = UFImportUEPose.bl_idname
    bl_file_extensions = ".uepose"
