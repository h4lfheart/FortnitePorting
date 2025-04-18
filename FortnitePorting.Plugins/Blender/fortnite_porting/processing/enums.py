from enum import IntEnum, auto

class EImageFormat(IntEnum):
    PNG = 0
    TGA = 1
    
class ESoundFormat(IntEnum):
    WAV = 0
    MP3 = 1
    OGG = 2
    FLAC = 3


class EPolygonType(IntEnum):
    TRIANGLES = 0
    QUADS = 1


class EExportType(IntEnum):
    NONE = 0

    # COSMETICS
    OUTFIT = auto()
    CHARACTER_PART = auto()
    BACKPACK = auto()
    PICKAXE = auto()
    GLIDER = auto()
    PET = auto()
    TOY = auto()
    EMOTICON = auto()
    SPRAY = auto()
    BANNER = auto()
    LOADING_SCREEN = auto()
    EMOTE = auto()
    
    # CREATIVE
    PROP = auto()
    PREFAB = auto()
    
    # GAMEPLAY
    ITEM = auto()
    RESOURCE = auto()
    TRAP = auto()
    VEHICLE = auto()
    WILDLIFE = auto()
    WEAPON_MOD = auto()
    
    # FESTIVAL
    FESTIVAL_GUITAR = auto()
    FESTIVAL_BASS = auto()
    FESTIVAL_KEYTAR = auto()
    FESTIVAL_DRUM = auto()
    FESTIVAL_MIC = auto()
    
    # LEGO
    LEGO_OUTFIT = auto()
    LEGO_EMOTE = auto()
    LEGO_PROP = auto()
    LEGO_WILDLIFE = auto()
    
    # FALL GUYS
    FALL_GUYS_OUTFIT = auto()
    
    # GENERIC
    MESH = auto()
    WORLD = auto()
    TEXTURE = auto()
    ANIMATION = auto()
    SOUND = auto()
    FONT = auto()
    POSE_ASSET = auto()
    MATERIAL = auto()
    MATERIAL_INSTANCE = auto()


class EPrimitiveExportType(IntEnum):
    MESH = 0
    ANIMATION = auto()
    TEXTURE = auto()
    SOUND = auto()
    FONT = auto()
    POSE_ASSET = auto()
    MATERIAL = auto()


class EFortCustomPartType(IntEnum):
    HEAD = 0
    BODY = 1
    HAT = 2
    BACKPACK = 3
    MISC_OR_TAIL = 4
    FACE = 5
    GAMEPLAY = 6
    
    NONE = -1

    @classmethod
    def _missing_(cls, value):
        return EFortCustomPartType.NONE

class ETextureImportMethod(IntEnum):
    DATA = 0
    OBJECT = auto()

class EMaterialImportMethod(IntEnum):
    DATA = 0
    OBJECT = auto()
    
class ERigType(IntEnum):
    DEFAULT = 0
    TASTY = auto()
    
class EOpElementType(IntEnum):
    OPERATOR = 0
    NAME = 1
    FUNCTION_REF = 2
    FLOAT = 3
    
class EOperator(IntEnum):
    NEGATE = 0
    ADD = auto()
    SUBTRACT = auto()
    MULTIPLY = auto()
    DIVIDE = auto()
    MODULO = auto()
    POWER = auto()
    FLOOR_DIVIDE = auto()