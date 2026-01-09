from .base_context import BaseImportContext
from .mesh_context import MeshImportContext
from .texture_context import TextureImportContext
from .material_context import MaterialImportContext
from .sound_context import SoundImportContext
from .anim_context import AnimImportContext
from .font_context import FontImportContext
from .pose_context import PoseImportContext

class ImportContext(BaseImportContext, MeshImportContext, MaterialImportContext, 
                   AnimImportContext, TextureImportContext, SoundImportContext, 
                   FontImportContext, PoseImportContext):
    
    def __init__(self, meta_data):
        BaseImportContext.__init__(self, meta_data)

__all__ = [
    'ImportContext',
    'BaseImportContext',
    'MeshImportContext',
    'TextureImportContext',
    'MaterialImportContext',
    'SoundImportContext',
    'AnimImportContext',
    'FontImportContext',
    'PoseImportContext',
]