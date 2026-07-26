from ..context import (
    AnimImportContext,
    BaseImportContext,
    FontImportContext,
    MeshImportContext,
    PoseImportContext,
    SoundImportContext,
    TastyImportContext,
    TextureImportContext,
)
from .data import ensure_legacy_blend_data
from .material_context import LegacyMaterialImportContext


class LegacyImportContext(
    BaseImportContext,
    MeshImportContext,
    AnimImportContext,
    TextureImportContext,
    SoundImportContext,
    FontImportContext,
    PoseImportContext,
    TastyImportContext,
    LegacyMaterialImportContext,
):
    """Uses the modern import pipeline with Blender 4.2-compatible materials."""

    def __init__(self, meta_data):
        BaseImportContext.__init__(self, meta_data)
        LegacyMaterialImportContext.__init__(self)

    def load_blend_data(self):
        ensure_legacy_blend_data()
