from enum import IntEnum, auto

class EImageFormat(IntEnum):
    PNG = 0
    TGA = 1


class EExportType(IntEnum):
    NONE = 0

    # COSMETICS
    OUTFIT = auto()
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
    
    # GENERIC
    MESH = auto()
    WORLD = auto()
    TEXTURE = auto()
    ANIMATION = auto()
    SOUND = auto()


class EPrimitiveExportType(IntEnum):
    MESH = 0
    ANIMATION = auto()
    TEXTURE = auto()
    SOUND = auto()


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
