from enum import IntEnum, auto


class EExportType(IntEnum):
    NONE = 0

    OUTFIT = auto()
    BACKPACK = auto()


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
    MISCORTAIL = 4
    FACE = 5
    GAMEPLAY = 6
