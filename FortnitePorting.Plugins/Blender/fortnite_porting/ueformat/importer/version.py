from enum import IntEnum, auto


class EUEFormatVersion(IntEnum):
    BeforeCustomVersionWasAdded = 0
    SerializeBinormalSign = 1
    AddMultipleVertexColors = 2
    AddConvexCollisionGeom = 3
    LevelOfDetailFormatRestructure = 4
    SerializeVirtualBones = 5
    SerializeMaterialPath = 6
    SerializeAssetMetadata = 7
    PreserveOriginalTransforms = 8
    AddPoseExport = 9
    AttributeFormatRestructure = 10

    VersionPlusOne = auto()
    LatestVersion = VersionPlusOne - 1
