from enum import IntEnum, auto


class EBlendMode(IntEnum):
    BLEND_Opaque = 0
    BLEND_Masked = auto()
    BLEND_Translucent = auto()
    BLEND_Additive = auto()
    BLEND_Modulate = auto()
    BLEND_AlphaComposite = auto()
    BLEND_AlphaHoldout = auto()
    BLEND_MAX = auto()


class ETranslucencyLightingMode(IntEnum):
    TLM_VolumetricNonDirectional = 0
    TLM_VolumetricDirectional = auto()
    TLM_VolumetricPerVertexNonDirectional = auto()
    TLM_VolumetricPerVertexDirectional = auto()
    TLM_Surface = auto()
    TLM_SurfacePerPixelLighting = auto()
    TLM_MAX = auto()


class EMaterialShadingModel(IntEnum):
    MSM_Unlit = 0
    MSM_DefaultLit = auto()
    MSM_Subsurface = auto()
    MSM_PreintegratedSkin = auto()
    MSM_ClearCoat = auto()
    MSM_SubsurfaceProfile = auto()
    MSM_TwoSidedFoliage = auto()
    MSM_Hair = auto()
    MSM_Cloth = auto()
    MSM_Eye = auto()
    MSM_SingleLayerWater = auto()
    MSM_ThinTranslucent = auto()
    MSM_Strata = auto()
    MSM_NUM = auto()
    MSM_FromMaterialExpression = auto()
    MSM_MAX = auto()
