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


class ExportCategory(IntEnum):
    COSMETIC = 1 << 8
    CREATIVE = 2 << 8
    GAMEPLAY = 3 << 8
    FESTIVAL = 4 << 8
    LEGO = 5 << 8
    FALL_GUYS = 6 << 8
    GENERIC = 7 << 8
    UTILITY = 8 << 8


class EExportType(IntEnum):
    NONE = 0

    # COSMETICS
    OUTFIT = ExportCategory.COSMETIC + 1
    CHARACTER_PART = ExportCategory.COSMETIC + 2
    BACKPACK = ExportCategory.COSMETIC + 3
    PICKAXE = ExportCategory.COSMETIC + 4
    GLIDER = ExportCategory.COSMETIC + 5
    PET = ExportCategory.COSMETIC + 6
    TOY = ExportCategory.COSMETIC + 7
    EMOTICON = ExportCategory.COSMETIC + 8
    SPRAY = ExportCategory.COSMETIC + 9
    BANNER = ExportCategory.COSMETIC + 10
    LOADING_SCREEN = ExportCategory.COSMETIC + 11
    EMOTE = ExportCategory.COSMETIC + 12
    SIDEKICK = ExportCategory.COSMETIC + 13
    KICKS = ExportCategory.COSMETIC + 14
    SPRITE = ExportCategory.COSMETIC + 15

    # CREATIVE
    PROP = ExportCategory.CREATIVE + 1
    PREFAB = ExportCategory.CREATIVE + 2

    # GAMEPLAY
    ITEM = ExportCategory.GAMEPLAY + 1
    RESOURCE = ExportCategory.GAMEPLAY + 2
    TRAP = ExportCategory.GAMEPLAY + 3
    VEHICLE = ExportCategory.GAMEPLAY + 4
    WILDLIFE = ExportCategory.GAMEPLAY + 5
    WEAPON_MOD = ExportCategory.GAMEPLAY + 6

    # FESTIVAL
    FESTIVAL_GUITAR = ExportCategory.FESTIVAL + 1
    FESTIVAL_BASS = ExportCategory.FESTIVAL + 2
    FESTIVAL_KEYTAR = ExportCategory.FESTIVAL + 3
    FESTIVAL_DRUM = ExportCategory.FESTIVAL + 4
    FESTIVAL_MIC = ExportCategory.FESTIVAL + 5

    # LEGO
    LEGO_OUTFIT = ExportCategory.LEGO + 1
    LEGO_EMOTE = ExportCategory.LEGO + 2
    LEGO_PROP = ExportCategory.LEGO + 3
    LEGO_WILDLIFE = ExportCategory.LEGO + 4

    # FALL GUYS
    FALL_GUYS_OUTFIT = ExportCategory.FALL_GUYS + 1

    # GENERIC
    MESH = ExportCategory.GENERIC + 1
    WORLD = ExportCategory.GENERIC + 2
    TEXTURE = ExportCategory.GENERIC + 3
    ANIMATION = ExportCategory.GENERIC + 4
    SOUND = ExportCategory.GENERIC + 5
    FONT = ExportCategory.GENERIC + 6
    POSE_ASSET = ExportCategory.GENERIC + 7
    MATERIAL = ExportCategory.GENERIC + 8
    MATERIAL_INSTANCE = ExportCategory.GENERIC + 9

    # UTILITY
    TASTY_RIG = ExportCategory.UTILITY + 1


class EPrimitiveExportType(IntEnum):
    MESH = 0
    ANIMATION = auto()
    TEXTURE = auto()
    SOUND = auto()
    FONT = auto()
    POSE_ASSET = auto()
    MATERIAL = auto()

    # UTILITY
    TASTY_RIG = auto()


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