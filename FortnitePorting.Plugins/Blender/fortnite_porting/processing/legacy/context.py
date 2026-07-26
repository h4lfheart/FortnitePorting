from ..enums import EExportType as CurrentExportType
from ..enums import EPrimitiveExportType as CurrentPrimitiveExportType
from .enums import EExportType as LegacyExportType
from .enums import EPrimitiveExportType as LegacyPrimitiveExportType
from .import_context import LegacyMaterialImportContext


class LegacyImportContext(LegacyMaterialImportContext):
    """Imports on Blender 4.2-4.5 using the compatible V3 asset library."""

    def run(self, data):
        legacy_data = dict(data)

        try:
            export_name = CurrentExportType(data.get("Type")).name
            legacy_data["Type"] = LegacyExportType[export_name].value

            primitive_name = CurrentPrimitiveExportType(data.get("PrimitiveType")).name
            legacy_data["PrimitiveType"] = LegacyPrimitiveExportType[primitive_name].value
        except (KeyError, ValueError) as exc:
            raise ValueError(
                "This export type is not supported by the Blender 4.2-compatible import pipeline."
            ) from exc

        super().run(legacy_data)
