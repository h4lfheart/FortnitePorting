from .enums import *
from .mappings_registry import *
from .names import *
from .utils import *


# Start base groups (default, eye, toon, layer, etc)
@registry.register
class DefaultMappings(MappingCollection):
    node_name="FPv4 Base Material"
    type=ENodeType.NT_Base

    @classmethod
    def meets_criteria(self, material_data):
        return False


    textures=(
        SlotMapping("Diffuse"),
        SlotMapping("D", "Diffuse"),
        SlotMapping("Base Color", "Diffuse"),
        SlotMapping("BaseColor", "Diffuse"),
        SlotMapping("Concrete", "Diffuse"),
        SlotMapping("Trunk_BaseColor", "Diffuse"),
        SlotMapping("Diffuse Top", "Diffuse"),
        SlotMapping("BaseColor_Trunk", "Diffuse"),
        SlotMapping("CliffTexture", "Diffuse"),
        SlotMapping("PM_Diffuse", "Diffuse"),
        SlotMapping("___Diffuse", "Diffuse"),
        SlotMapping("BaseColorTexture", "Diffuse"),
        SlotMapping("BaseColor Map", "Diffuse"),

        SlotMapping("Background Diffuse", alpha_slot="Background Diffuse Alpha"),
        SlotMapping("BG Diffuse Texture", "Background Diffuse", alpha_slot="Background Diffuse Alpha"),

        SlotMapping("M"),
        SlotMapping("Mask", "M"),
        SlotMapping("M Mask", "M"),

        SlotMapping("SpecularMasks"),
        SlotMapping("S", "SpecularMasks"),
        SlotMapping("SRM", "SpecularMasks", switch_slot="SwizzleRoughnessToGreen"),
        SlotMapping("S Mask", "SpecularMasks"),
        SlotMapping("Specular Mask", "SpecularMasks"),
        SlotMapping("SpecularMask", "SpecularMasks"),
        SlotMapping("Concrete_SpecMask", "SpecularMasks"),
        SlotMapping("Trunk_Specular", "SpecularMasks"),
        SlotMapping("Specular Top", "SpecularMasks"),
        SlotMapping("SMR_Trunk", "SpecularMasks"),
        SlotMapping("Cliff Spec Texture", "SpecularMasks"),
        SlotMapping("PM_SpecularMasks", "SpecularMasks"),
        SlotMapping("__PBR Masks", "SpecularMasks"),
        SlotMapping("MetallicRoughnessTexture", "SpecularMasks"),
        SlotMapping("Bake Packed Maps", "SpecularMasks"),

        SlotMapping("Normals"),
        SlotMapping("N", "Normals"),
        SlotMapping("Normal", "Normals"),
        SlotMapping("NormalMap", "Normals"),
        SlotMapping("ConcreteTextureNormal", "Normals"),
        SlotMapping("Trunk_Normal", "Normals"),
        SlotMapping("Normals Top", "Normals"),
        SlotMapping("Normal_Trunk", "Normals"),
        SlotMapping("CliffNormal", "Normals"),
        SlotMapping("PM_Normals", "Normals"),
        SlotMapping("_Normal", "Normals"),
        SlotMapping("NormalTexture", "Normals"),
        SlotMapping("Normal Map", "Normals"),
        SlotMapping("Baked Normal", "Normals"),

        SlotMapping("Emission"),
        SlotMapping("Emissive", "Emission"),
        SlotMapping("EmissiveColor", "Emission"),
        SlotMapping("EmissiveTexture", "Emission"),
        SlotMapping("L1_Emissive", "Emission"),
        SlotMapping("PM_Emissive", "Emission"),

        SlotMapping("MaskTexture"),
        SlotMapping("OpacityMask", "MaskTexture"),
        SlotMapping("MessHairMask", "MaskTexture"),

        SlotMapping("FX Mask"),
        SlotMapping("FX", "FX Mask"),
        SlotMapping("SkinFX_Mask", "FX Mask"),
        SlotMapping("SkinFX Mask", "FX Mask"),
        SlotMapping("TechArtMask", "FX Mask"),
        SlotMapping("FxMask", "FX Mask"),
        SlotMapping("FX_Mask", "FX Mask"),
    )

    scalars=(
        SlotMapping("RoughnessMin", "Roughness Min"),
        SlotMapping("SpecRoughnessMin", "Roughness Min"),
        SlotMapping("RawRoughnessMin", "Roughness Min"),
        SlotMapping("Rough Min", "Roughness Min"),
        SlotMapping("RoughnessMax", "Roughness Max"),
        SlotMapping("SpecRoughnessMax", "Roughness Max"),
        SlotMapping("RawRoughnessMax", "Roughness Max"),
        SlotMapping("Rough Max", "Roughness Max"),

        SlotMapping("EmissiveBrightness", "Emission Strength"),
        SlotMapping("emissive mult", "Emission Strength"),
        SlotMapping("DayMult", "Emission Strength"),
    )

    colors=(
        SlotMapping("TintColor", "Background Diffuse"),
        SlotMapping("BaseColorFactor", "Background Diffuse"),

        SlotMapping("Emissive", "Emission Multiplier"),
        SlotMapping("EmissiveMultiplier", "Emission Multiplier"),
        SlotMapping("Emissive Multiplier", "Emission Multiplier"),

        SlotMapping("Emissive Color", "Emission Color", switch_slot="Use Emission Color"),
        SlotMapping("EmissiveColor", "Emission Color", switch_slot="Use Emission Color"),
    )

    switches=(
        SlotMapping("SwizzleRoughnessToGreen"),
    )


@registry.register
class BaseLayerMappings(MappingCollection):
    node_name="FPv4 Base Layer"
    type=ENodeType.NT_Base

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), layer_switch_names) and get_param_multiple(material_data.get("Textures"), extra_layer_names)


    textures=(
        SlotMapping("Diffuse", alpha_slot="MaskTexture"),
        SlotMapping("SpecularMasks"),
        SlotMapping("Normals"),
        SlotMapping("EmissiveTexture"),
        SlotMapping("MaskTexture"),
        SlotMapping("Background Diffuse", alpha_slot="Background Diffuse Alpha"),
    )

    colors=(
        SlotMapping("TintColor", "Background Diffuse"),
    )


@registry.register
class BaseEyeMappings(MappingCollection):
    node_name="FPv4 3L Eyes"
    type=ENodeType.NT_Base
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return any(eye_names, lambda eye_mat_name: eye_mat_name in material_data.get("BaseMaterialPath")) or get_param(material_data.get("Scalars"), "Eye Cornea IOR") is not None


    textures=(
        SlotMapping("Diffuse", closure=True),
        SlotMapping("Normal", default=DefaultTexture("FlatNormal", False), closure=True),
        SlotMapping("SpecularMasks", closure=True),
        SlotMapping("SRM", "SpecularMasks", switch_slot="SwizzleRoughnessToGreen", closure=True), # TODO: Default SMR?
        SlotMapping("Emissive", closure=True),
    )

    colors=(
        SlotMapping("EyeTintColor"),
    )

    vectors=(
        SlotMapping("Eye Right UV Position", default=(0.94, 0.122, 0.0, 0.0)),
        SlotMapping("Eye Left UV Position", default=(0.94, 0.35, 0.0, 0.0)),
        SlotMapping("Eye Right UV Position (UV0)", "Eye Right UV Position"),
        SlotMapping("Eye Left UV Position (UV0)", "Eye Left UV Position"),

        SlotMapping("Eye Camera Light Vector", default=(-0.2, 0.075, -0.5, 0.0)),
        SlotMapping("Eye UV Highlight Pos", default=(0.92, 0.12, 0.0, 0.0)),
    )

    scalars=(
        SlotMapping("Eye Roughness Min"),
        SlotMapping("Eye Metallic Mult"),

        SlotMapping("Emissive Mult"),
        SlotMapping("Eye Texture AspectRatio"),
        SlotMapping("Eye Cornea Radius (UV)"),

        SlotMapping("Eye UV Highlight Size"),

        SlotMapping("Eye Iris Normal Flatten"),
        SlotMapping("EyeTintMask_Radius"),

        SlotMapping("Eye Cornea Mask Hardness"),
        SlotMapping("Eye Iris UV Radius"),
        SlotMapping("Eye Refraction Mix"),
        SlotMapping("Eye Refraction Mult"),
        SlotMapping("Eye Iris Depth Scale"),
        SlotMapping("Eye Cornea IOR"),
    )

    switches=(
        SlotMapping("SwizzleRoughnessToGreen"),
        SlotMapping("Eye Use Sun Highlight"),
        SlotMapping("Eye Use UV Highlight"),
        SlotMapping("UseEyeColorTinting"),
    )


@registry.register
class BaseToonMappings(MappingCollection):
    node_name="FPv4 Base Toon"
    type=ENodeType.NT_Base

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Textures"), toon_texture_names) or get_param_multiple(material_data.get("Vectors"), toon_vector_names)


    textures=(
        SlotMapping("LitDiffuse"),
        SlotMapping("Color_Lit_Map", "LitDiffuse"),
        SlotMapping("ShadedDiffuse"),
        SlotMapping("Color_Shaded_Map", "ShadedDiffuse"),
        SlotMapping("Diffuse", "ShadedDiffuse"),
        SlotMapping("DistanceField_InkLines"),
        SlotMapping("DFL_Map", "DistanceField_InkLines"),
        SlotMapping("InkLineColor_Texture"),
        SlotMapping("DFL_Color_Map", "InkLineColor_Texture"),
        SlotMapping("SSC_Texture"),
        SlotMapping("STM_Map", "SSC_Texture"),
        SlotMapping("STT_Map"),
        SlotMapping("Normals"),
        SlotMapping("Normal_Map", "Normals")
    )

    scalars=(
        SlotMapping("ShadedColorDarkening"),
        SlotMapping("FakeNormalBlend_Amt"),
        SlotMapping("VertexBakedNormal_Blend", "FakeNormalBlend_Amt"),
        SlotMapping("PBR_Shading", "Use PBR Shading", value_func=lambda value: int(value))
    )

    colors=(
        SlotMapping("InkLineColor", "InkLineColor_Texture"),
        SlotMapping("Color_Lit", "LitDiffuse"),
        SlotMapping("Color_Shaded", "ShadedDiffuse"),
        SlotMapping("SpecularTint"),
        SlotMapping("Specular Tint", "SpecularTint"),
    )

    switches=(
        SlotMapping("UseFakePBR", "Use PBR Shading"),
    )


@registry.register
class BaseBeanMappings(MappingCollection):
    node_name="FPv4 Base Bean"
    type=ENodeType.NT_Base

    @classmethod
    def meets_criteria(self, material_data):
        return "MM_BeanCharacter_Body" in material_data.get("BaseMaterialPath")


    textures=(
        SlotMapping("Body_Pattern", closure=True),
    )

    colors=(
        SlotMapping("Body_EyesColor"),
        SlotMapping("Body_MainColor"),
        SlotMapping("Body_SecondaryColor"),
        SlotMapping("Body_FacePlateColor"),
        SlotMapping("Body_Eyes_MaterialProps"),
        SlotMapping("Body_Faceplate_MaterialProps"),
        SlotMapping("Body_GlassesEyeLashes"),
        SlotMapping("Body_MaterialProps"),
        SlotMapping("Body_Secondary_MaterialProps"),
        SlotMapping("Eyelashes_Color"),
        SlotMapping("Eyelashes_MaterialProps"),
        SlotMapping("Glasses_Frame_Color"),
        SlotMapping("Glasses_Frame_MaterialProps"),
        SlotMapping("Body_EyesColor"),
        SlotMapping("Glasses_Lense_Color"),
        SlotMapping("Glasses_Lense_MaterialProps"),
    )


@registry.register
class BaseBeanCostumeMappings(MappingCollection):
    node_name="FPv4 Base Bean Costume"
    type=ENodeType.NT_Base

    @classmethod
    def meets_criteria(self, material_data):
        return "MM_BeanCharacter_Costume" in material_data.get("BaseMaterialPath")


    textures=(
        SlotMapping("Metalness/Roughness/Specular/Albedo", "Metalness/Roughness/Specular", alpha_slot="Albedo"),
        SlotMapping("MaterialMasking", closure=True),
        SlotMapping("NormalMap"),
    )

    colors=(
        SlotMapping("Costume_MainColor"),
        SlotMapping("Head_Costume_MainColor", "Costume_MainColor"),
        SlotMapping("Costume_MainMaterialProps"),
        SlotMapping("Head_Costume_MainMaterialProps", "Costume_MainMaterialProps"),
        SlotMapping("Costume_Secondary_Color"),
        SlotMapping("Head_Costume_Secondary_Color", "Costume_Secondary_Color"),
        SlotMapping("Costume_SecondaryMaterialProps"),
        SlotMapping("Head_Costume_SecondaryMaterialProps", "Costume_SecondaryMaterialProps"),
        SlotMapping("Costume_AccentColor"),
        SlotMapping("Head_Costume_AccentColor", "Costume_AccentColor"),
        SlotMapping("Costume_AccentMaterialProps"),
        SlotMapping("Head_Costume_AccentMaterialProps", "Costume_AccentMaterialProps"),
    )

    vectors=(
        SlotMapping("Costume_UVPatternPosition"),
        SlotMapping("Head_Costume_UVPatternPosition", "Costume_UVPatternPosition"),
    )
# End base groups

# Start Layer groups
class ParentLayerMappings(LayerMappingsTemplate):
    node_name="FPv4 Layer"
    type=ENodeType.NT_Layer

    @classmethod
    def meets_criteria_dynamic(self, material_data, index):
        return get_param_multiple(material_data.get("Switches"), [f"Use {index} Layers", f"Use {index} Materials"])


    LAYER_TEXTURE_TEMPLATES = (
        SlotMapping("Diffuse_Texture_#", "Diffuse", alpha_slot="MaskTexture"),
        SlotMapping("SpecularMasks_#", "SpecularMasks"),
        SlotMapping("Normals_Texture_#", "Normals"),
        SlotMapping("Emissive_Texture_#", "EmissiveTexture"),
        SlotMapping("MaskTexture_#", "MaskTexture"),
        SlotMapping("Background Diffuse #", "Background Diffuse", alpha_slot="Background Diffuse Alpha"),
    )

    LAYER_SWITCH_TEMPLATES = (
        SlotMapping("Use # Layers", "Use Layer"),
        SlotMapping("Use # Materials", "Use Layer"),
    )

    @classmethod
    def scalars(self, index):
        return (SlotMapping("Layer", default=index),)


create_layer_mappings(ParentLayerMappings, "")
# End Layer groups

# Start basic FX groups
@registry.register
class SkinMappings(MappingCollection):
    node_name="FPv4 Skin"
    type=ENodeType.NT_Core_FX
    order=0

    colors=(
        SlotMapping("Skin Boost Color And Exponent", "Skin Color", alpha_slot="Skin Boost"),
        SlotMapping("SkinTint", "Skin Color", alpha_slot="Skin Boost"),
        SlotMapping("SkinColor", "Skin Color", alpha_slot="Skin Boost"),
    )


@registry.register
class CroppedEmissiveMappings(MappingCollection):
    node_name="FPv4 CroppedEmissive"
    type=ENodeType.NT_Core_FX
    order=1
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return any(get_params(material_data.get("Switches"), emissive_crop_switch_names), lambda bool: bool is True) \
            and get_param_multiple(material_data.get("Vectors"), emissive_crop_vector_names) is not None \
            and get_param_multiple(material_data.get("Textures"), ["Emissive", "CroppedEmissive"]) is not None

    textures=(
        SlotMapping("Emissive", closure=True),
        SlotMapping("CroppedEmissive", "Emissive", closure=True),
    )

    scalars=(
        SlotMapping("EmissiveBrightness", "Emission Strength"),
        SlotMapping("emissive mult", "Emission Strength"),
        SlotMapping("DayMult", "Emission Strength"),
    )

    colors=(
        SlotMapping("Emissive Color", "Emission Multiplier"),
        SlotMapping("EmissiveColor", "Emission Multiplier"),
        SlotMapping("EmissiveMultiplier", "Emission Multiplier"),
        SlotMapping("Emissive Multiplier", "Emission Multiplier"),
        SlotMapping("Emissive", "Emission Multiplier"),
    )

    vectors=(
        SlotMapping("CroppedEmissiveUVs", default=(0.0, 0.0, 1.0, 1.0)),
        SlotMapping("EmissiveUVs_RG_UpperLeftCorner_BA_LowerRightCorner", "CroppedEmissiveUVs"),
        SlotMapping("Emissive Texture UVs RG_TopLeft BA_BottomRight", "CroppedEmissiveUVs"),
        SlotMapping("Emissive 2 UV Positioning (RG)UpperLeft (BA)LowerRight", "CroppedEmissiveUVs"),
        SlotMapping("EmissiveUVPositioning (RG)UpperLeft (BA)LowerRight", "CroppedEmissiveUVs"),
    )

    switches=(
        SlotMapping("CroppedEmissive", "Use Cropped Emission"),
        SlotMapping("UseCroppedEmissive", "Use Cropped Emission"),
        SlotMapping("Manipulate Emissive Uvs", "Use Cropped Emission"),
    )


@registry.register
class EmissiveComponentMappings(MappingCollection):
    node_name="FPv4 EmissiveComponent"
    type=ENodeType.NT_Core_FX
    order=1.1

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("ComponentMasks"), ["EmissiveComponentMask", "Emissive Component Mask"]) is not None

    textures=(
        SlotMapping("Emissive"),
        SlotMapping("Emission", "Emissive"),
        SlotMapping("EmissiveColor", "Emissive"),
        SlotMapping("EmissiveTexture", "Emissive"),
        SlotMapping("L1_Emissive", "Emissive"),
        SlotMapping("PM_Emissive", "Emissive"),
    )

    scalars=(
        SlotMapping("EmissiveBrightness", "Emission Strength"),
        SlotMapping("emissive mult", "Emission Strength"),
        SlotMapping("DayMult", "Emission Strength"),
    )

    colors=(
        SlotMapping("Emissive Color", "Emission Multiplier"),
        SlotMapping("EmissiveColor", "Emission Multiplier"),
        SlotMapping("EmissiveMultiplier", "Emission Multiplier"),
        SlotMapping("Emissive Multiplier", "Emission Multiplier"),
        SlotMapping("Emissive", "Emission Multiplier"),
    )

    component_masks=(
        SlotMapping("EmissiveComponentMask", switch_slot="Use Emissive Component Mask"),
        SlotMapping("Emissive Component Mask", "EmissiveComponentMask", switch_slot="Use Emissive Component Mask"),
    )


@registry.register
class DistanceFieldMappings(MappingCollection):
    node_name="FPv4 DistanceField"
    type=ENodeType.NT_Core_FX
    order=1.2
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return get_param(material_data.get("Switches"), "UseAnimatedEmissive") and get_param(material_data.get("Textures"), "EmissiveDistanceField") is not None

    textures=(
        SlotMapping("EmissiveDistanceField", closure=True),
    )

    scalars=(
        SlotMapping("SubUV_Frames"),
        SlotMapping("SubUV_Speed"),
    )

    switches=(
        SlotMapping("UseAnimatedEmissive"),
    )


@registry.register
class VisorMappings(MappingCollection):
    node_name="FPv4 Visor"
    type=ENodeType.NT_Core_FX
    order=1.3
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return (get_param(material_data.get("Switches"), "UseAnimatedVisor") or "M_MED_Turtleneck_MaskFX_Master" in material_data.get("BaseMaterialPath")) \
            and get_param_multiple(material_data.get("Textures"), ["Visor_Emissive", "EmissiveGradient"]) is not None \
            and get_param_multiple(material_data.get("Textures"), ["Visor_EmissiveDistanceField", "DF Texture"]) is not None

    textures=(
        SlotMapping("Visor_Emissive", closure=True),
        SlotMapping("EmissiveGradient", "Visor_Emissive", closure=True),
        SlotMapping("Visor_EmissiveDistanceField", closure=True),
        SlotMapping("DF Texture", "Visor_EmissiveDistanceField", switch_slot="UseAnimatedVisor", closure=True),
    )

    scalars=(
        SlotMapping("Visor_SubUV_Frames"),
        SlotMapping("SubUV_Frames", "Visor_SubUV_Frames"),
        SlotMapping("Visor_SubUV_Speed"),
        SlotMapping("SubUV_Speed", "Visor_SubUV_Speed"),
        SlotMapping("Visor_LineThick"),
        SlotMapping("TN_SurfaceDistanceOffset", "Visor_LineThick"),
        SlotMapping("VisorHeight"),
        SlotMapping("TN_Ly1Depth", "VisorHeight"),
        SlotMapping("VisorLayerHeightRatio"),
        SlotMapping("Visor_Layer1_Strength"),
        SlotMapping("Visor_Layer2_Strength"),
        SlotMapping("TN_Ly2Depth_SideMult", "Visor_Layer2_Strength"),
    )

    colors=(
        SlotMapping("Emissive Color"),
        SlotMapping("TN_Emissive Color", "Emissive Color"),
    )

    switches=(
        SlotMapping("UseAnimatedVisor"),
    )


@registry.register
class EmissiveFresnelMappings(MappingCollection):
    node_name="FPv4 EmissiveFresnel"
    type=ENodeType.NT_Core_FX
    order=1.4

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), ["Use Emissive Fresnel", "UseEmissiveFresnel", "CameraFacing"])

    scalars=(
        SlotMapping("Invert Emissive Fresnel", "InvertEmissiveFresnel"),
        SlotMapping("CameraFacing_Inverted", "InvertEmissiveFresnel", value_func=lambda value: int(1 - value)),

        SlotMapping("EmissiveFresnelPower"),
        SlotMapping("Fresnel_Power", "EmissiveFresnelPower"),
        SlotMapping("Emissive Fres EX", "EmissiveFresnelPower"),
    )

    switches=(
        SlotMapping("Use Emissive Fresnel"),
        SlotMapping("UseEmissiveFresnel", "Use Emissive Fresnel"),
        SlotMapping("CameraFacing", "Use Emissive Fresnel"),
        SlotMapping("InvertEmissiveFresnel"),
    )


@registry.register
class EmissiveFXMaskMappings(MappingCollection):
    node_name="FPv4 EmissiveFXMask"
    type=ENodeType.NT_Core_FX
    order=1.5

    @classmethod
    def meets_criteria(self, material_data):
        return get_param(material_data.get("Switches"), "UseFXMaskForEmissive")

    colors=(
        SlotMapping("EmissiveFXMaskChannel")
    )

    switches=(
        SlotMapping("UseFXMaskForEmissive"),
    )


@registry.register
class ClothFuzzMappings(MappingCollection):
    node_name="FPv4 ClothFuzz"
    type=ENodeType.NT_Core_FX
    order=2
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), ["Use Cloth Fuzz", "UseClothFuzz"])

    textures=(
        SlotMapping("ClothFuzz Texture", default=DefaultTexture("T_Fuzz_MASK"), closure=True),
    )

    scalars=(
        SlotMapping("Fuzz Tiling"),
        SlotMapping("ClothFuzzTiling", "Fuzz Tiling"),
        SlotMapping("Fuzz Exponent"),
        SlotMapping("ClothFuzzExponent", "Fuzz Exponent"),
        SlotMapping("Fuzz Fresnel Blend"),
        SlotMapping("Cloth Base Color Intensity"),
        SlotMapping("Cloth_BaseColorIntensity", "Cloth Base Color Intensity"),
        SlotMapping("Cloth Roughness"),
        SlotMapping("Cloth_Roughness", "Cloth Roughness"),
    )

    colors=(
        SlotMapping("Cloth Channel"),
        SlotMapping("ClothFuzzMaskChannel", "Cloth Channel"),

        SlotMapping("ClothFuzz Tint"),
        SlotMapping("Fuzz Tint", "ClothFuzz Tint"),
        SlotMapping("ClothFuzzTint", "ClothFuzz Tint"),
        SlotMapping("Cloth Fuzz Tint", "ClothFuzz Tint"),
    )

    switches=(
        SlotMapping("Use Cloth Fuzz"),
        SlotMapping("UseClothFuzz", "Use Cloth Fuzz"),
    )

    component_masks=(
        SlotMapping("Cloth Channel"),
        SlotMapping("ClothFuzzMaskChannel", "Cloth Channel"),
    )


@registry.register
class SilkMappings(MappingCollection):
    node_name="FPv4 Silk"
    type=ENodeType.NT_Core_FX
    order=3

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), ["Use Silk", "UseSilk"])

    scalars=(
        SlotMapping("Silk Fresnel"),
        SlotMapping("SilkFresnelMin"),
        SlotMapping("SilkFresnelMax"),
        SlotMapping("SilkEdgeAniso"),
        SlotMapping("SilkBaseColorBrightness"),
    )

    colors=(
        SlotMapping("SilkMaskChannel"),
        SlotMapping("Silk_Channel", "SilkMaskChannel"),

        SlotMapping("SilkEdgeTint"),
    )

    switches=(
        SlotMapping("Use Silk"),
        SlotMapping("UseSilk", "Use Silk"),
    )

    component_masks=(
        SlotMapping("SilkMaskChannel"),
        SlotMapping("Silk_Channel", "SilkMaskChannel"),
    )


@registry.register
class ThinFilmMappings(MappingCollection):
    node_name="FPv4 ThinFilm"
    type=ENodeType.NT_Core_FX
    order=4
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), ["Use Thin Film", "UseThinFilm"])

    textures=(
        SlotMapping("ThinFilm_Texture", default=DefaultTexture("T_ThinFilm_Spectrum_COLOR"), closure=True),
        SlotMapping("ThinFilmTexture", "ThinFilm_Texture", closure=True),
        SlotMapping("Thin Film Texture", "ThinFilm_Texture", closure=True),
    )

    scalars=(
        SlotMapping("ThinFilm_Warp"),
        SlotMapping("ThinFilmWarp", "ThinFilm_Warp"),
        SlotMapping("ThinFilm_Scale"),
        SlotMapping("ThinFilmScale", "ThinFilm_Scale"),
        SlotMapping("ThinFilm_Offset"),
        SlotMapping("ThinFilmOffset", "ThinFilm_Offset"),
        SlotMapping("ThinFilm_Intensity"),
        SlotMapping("ThinFilmIntensity", "ThinFilm_Intensity"),
        SlotMapping("ThinFilm_Exponent"),
        SlotMapping("ThinFilmExponent", "ThinFilm_Exponent"),
        SlotMapping("RoughnessInfluence"),
        SlotMapping("ThinFilm_RoughnessScale", "RoughnessInfluence"),
        SlotMapping("ThinFilmRoughnessScale", "RoughnessInfluence"),
    )

    colors=(
        SlotMapping("ThinFilmMaskChannel"),
        SlotMapping("ThinFilm_Channel", "ThinFilmMaskChannel"),
    )

    switches=(
        SlotMapping("Use Thin Film"),
        SlotMapping("UseThinFilm", "Use Thin Film"),
    )

    component_masks=(
        SlotMapping("ThinFilmMaskChannel"),
        SlotMapping("ThinFilm_Channel", "ThinFilmMaskChannel"),
    )


@registry.register
class ThinFilm2Mappings(MappingCollection):
    node_name="FPv4 ThinFilm"
    type=ENodeType.NT_Core_FX
    order=4.1
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return get_param(material_data.get("Switches"), "UseThinFilm2")

    textures=(
        SlotMapping("ThinFilmTexture2", "ThinFilm_Texture", default=DefaultTexture("T_ThinFilm_Spectrum_COLOR"), closure=True),
    )

    scalars=(
        SlotMapping("ThinFilmWarp2", "ThinFilm_Warp"),
        SlotMapping("ThinFilmScale2", "ThinFilm_Scale"),
        SlotMapping("ThinFilmOffset2", "ThinFilm_Offset"),
        SlotMapping("ThinFilm_Intensity2", "ThinFilm_Intensity"),
        SlotMapping("ThinFilmExponent2", "ThinFilm_Exponent"),
        SlotMapping("RoughnessInfluence2", "RoughnessInfluence"),
    )

    colors=(
        SlotMapping("ThinFilm2MaskChannel", "ThinFilmMaskChannel"),
    )

    switches=(
        SlotMapping("UseThinFilm2", "Use Thin Film"),
    )

    component_masks=(
        SlotMapping("ThinFilm2MaskChannel", "ThinFilmMaskChannel"),
    )


@registry.register
class ClearCoatMappings(MappingCollection):
    node_name="FPv4 ClearCoat"
    type=ENodeType.NT_Core_FX
    order=5

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), ["Use Clear Coat", "UseClearCoat"])

    scalars=(
        SlotMapping("UnderCoatRoughness"),
        SlotMapping("Undercoat Roughness", "UnderCoatRoughness"),
        SlotMapping("UnderCoatMetallicMultiplier"),
        SlotMapping("Undercoat Metallic Multiplier", "UnderCoatMetallicMultiplier"),
        SlotMapping("Roughness Map Now affects Clearcoat roughness", "Use Roughness Map"),
    )

    colors=(
        SlotMapping("ClearCoatMaskChannel"),
        SlotMapping("Clear Coat Channel", "ClearCoatMaskChannel"),
        SlotMapping("ClearCoatChannel", "ClearCoatMaskChannel"),
        SlotMapping("CloatcoatMaskChannel", "ClearCoatMaskChannel"),
    )

    switches=(
        SlotMapping("Use Clear Coat"),
        SlotMapping("UseClearCoat", "Use Clear Coat"),
    )

    component_masks=(
        SlotMapping("ClearCoatMaskChannel"),
        SlotMapping("Clear Coat Channel", "ClearCoatMaskChannel"),
        SlotMapping("ClearCoatChannel", "ClearCoatMaskChannel"),
        SlotMapping("CloatcoatMaskChannel", "ClearCoatMaskChannel"),
    )


@registry.register
class MetalLUTMappings(MappingCollection):
    node_name="FPv4 MetalLUT"
    type=ENodeType.NT_Core_FX
    order=6

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), ["Use MetalLUT", "UseMetalLUT"])

    scalars=(
        SlotMapping("Metal LUT Curve"),
        SlotMapping("MetalLutIntensity"),
    )

    colors=(
        SlotMapping("MetalLUTMaskChannel"),
        SlotMapping("MetalLUT_Channel", "MetalLUTMaskChannel"),

        SlotMapping("LUTChannel"),
    )

    switches=(
        SlotMapping("Use MetalLUT"),
        SlotMapping("UseMetalLUT", "Use MetalLUT"),
    )

    component_masks=(
        SlotMapping("MetalLUTMaskChannel"),
        SlotMapping("MetalLUT_Channel", "MetalLUTMaskChannel"),
        SlotMapping("MetalMaskChannel", "MetalLUTMaskChannel"),

        SlotMapping("LUTChannel"),
    )

# TODO: Additional basic groups
# Gem - 7
# Subsurface - 8
# Advanced Emission - 9


@registry.register
class AnisotropicMappings(MappingCollection):
    node_name="FPv4 Anisotropic"
    type=ENodeType.NT_Core_FX
    order=10

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), ["Use AnisotropicShading", "UseAnisotropicShading"])

    textures=(
        SlotMapping("AnisotropicTangentWeight", alpha_slot="AnisotropicTangentWeight Alpha"),
        SlotMapping("AnisotropigTangentWeight", "AnisotropicTangentWeight", alpha_slot="AnisotropicTangentWeight Alpha"),
    )

    scalars=(
        SlotMapping("AnisotropyMaxWeight"),
    )

    switches=(
        SlotMapping("Use AnisotropicShading"),
        SlotMapping("UseAnisotropicShading", "Use AnisotropicShading"),
    )

# End basic FX groups

# Start advanced FX groups
@registry.register
class GlassMappings(MappingCollection):
    node_name="FPv4 Glass"
    type=ENodeType.NT_Advanced_FX
    surface_render_method="BLENDED"
    show_transparent_back=False

    @classmethod
    def meets_criteria(self, material_data):
        return material_data.get("PhysMaterialName") == "Glass" \
            or any(glass_master_names, lambda x: x in material_data.get("BaseMaterialPath")) \
            or (EBlendMode(material_data.get("BaseBlendMode")) is EBlendMode.BLEND_Translucent
                and ETranslucencyLightingMode(material_data.get("TranslucencyLightingMode")) in
                [ETranslucencyLightingMode.TLM_SurfacePerPixelLighting, ETranslucencyLightingMode.TLM_VolumetricPerVertexDirectional])


    textures=(
        SlotMapping("Color_DarkTint"),
        SlotMapping("GlassDiffuse"),
        SlotMapping("Diffuse Texture with Alpha Mask", "GlassDiffuse", alpha_slot="Mask"),
        SlotMapping("PM_Diffuse", "GlassDiffuse"),
    )

    scalars=(
        SlotMapping("Window Tint Amount", "Tint Amount"),

        SlotMapping("DiffuseTextureBlend"),

        SlotMapping("GlassSpecular"),
        SlotMapping("Specular", "GlassSpecular"),
        SlotMapping("GlassRoughness"),
        SlotMapping("Roughness", "GlassRoughness"),
        SlotMapping("GlassMetallic"),
        SlotMapping("Metallic", "GlassMetallic"),

        SlotMapping("SpecularTextureBlend"),
        SlotMapping("RoughnessTextureBlend"),
        SlotMapping("MetallicTextureBlend"),

        SlotMapping("Thickness"),
        SlotMapping("Glass thickness", "Thickness"),
        SlotMapping("GlassThickness", "Thickness"),
        SlotMapping("InnerTransparency"),
        SlotMapping("Inner Transparency", "InnerTransparency"),
        SlotMapping("Fresnel Inner Transparency", "InnerTransparency"),
        SlotMapping("Inner Transparency Max Tint"),
        SlotMapping("Fresnel Inner Transparency Max Tint", "Inner Transparency Max Tint"),
        SlotMapping("OuterTransparency"),
        SlotMapping("Outer Transparency", "OuterTransparency"),
        SlotMapping("Fresnel Outer Transparency", "OuterTransparency"),
        SlotMapping("FresnelExponentTransparency"),
        SlotMapping("Exponent", "FresnelExponentTransparency"),
        SlotMapping("Fresnel Exponent", "FresnelExponentTransparency"),

        SlotMapping("TextureOpacityAdd"),
        SlotMapping("TextureOpacityBlend"),
        SlotMapping("Mask"),
        SlotMapping("Mask Opacity"),
        SlotMapping("Alpha Channel Mask Opacity", "Mask Opacity"),
    )

    colors=(
        SlotMapping("GlassDiffuse"),
        SlotMapping("ColorFront", "GlassDiffuse"),
        SlotMapping("Base Color", "GlassDiffuse"),

        SlotMapping("TextureOpacityChannel"),
    )

    switches=(
        SlotMapping("useDiffuseMap", "Diffuse"),
        SlotMapping("useSRM", "Use SRM"),
        SlotMapping("UseTextureOpacity", "Texture Opacity"),
    )


@registry.register
class CompositeMappings(MappingCollection):
    node_name="FPv4 Composite"
    type=ENodeType.NT_Advanced_FX
    node_spacing=700

    textures=(
        SlotMapping("UV2Composite_AlphaTexture", closure=True),
        SlotMapping("UV2Composite_Diffuse", closure=True),
        SlotMapping("UV2Composite_Normals", closure=True),
        SlotMapping("UV2Composite_SRM", closure=True),
    )

    scalars=(
        SlotMapping("UV2Composite_AlphaStrength"),
    )

    colors=(
        SlotMapping("UV2Composite_AlphaChannel"),
    )

    switches=(
        SlotMapping("UseUV2Composite"),
        SlotMapping("UV2Composite_AlphaTextureUseUV1"),
        SlotMapping("UseTechArtMaskAsAlpha"),
        SlotMapping("UseDiffuseAlphaChannel"),
        SlotMapping("UseUV2Diffuse"),
        SlotMapping("UseUV2Normals"),
        SlotMapping("UseUV2SRM"),
    )


@registry.register
class DetailMappings(MappingCollection):
    node_name="FPv4 Detail"
    type=ENodeType.NT_Advanced_FX
    node_spacing=700

    textures=(
        SlotMapping("Detail Diffuse", closure=True),
        SlotMapping("Detail Normal", closure=True),
        SlotMapping("Detail SRM", closure=True),
    )

    scalars=(
        SlotMapping("Detail Texture - Tiling"),
        SlotMapping("Detail Texture - UV Rotation"),
        SlotMapping("Detail Diffuse - Strength"),
        SlotMapping("Detail Normal - Flatten Normal"),
        SlotMapping("Detail Specular - Strength"),
        SlotMapping("Detail Roughness - Strength"),
        SlotMapping("Detail Metallic - Strength"),
    )

    colors=(
        SlotMapping("Detail Texture - Channel Mask"),
    )

    switches=(
        SlotMapping("Detail Texture - Use UV2"),
        SlotMapping("Use Detail Diffuse"),
        SlotMapping("Use Detail Diffuse?", "Use Detail Diffuse"),
        SlotMapping("Use Detail Normal"),
        SlotMapping("Use Detail Normal?", "Use Detail Normal"),
        SlotMapping("Use Detail SRM"),
        SlotMapping("Use Detail SRM?", "Use Detail SRM"),
    )


# TODO: Separate nodes for toon eye/mouth/brow?
@registry.register
class FlipbookMappings(MappingCollection):
    node_name="FPv4 Flipbook"
    type=ENodeType.NT_Advanced_FX
    node_spacing=700

    textures=(
        SlotMapping("Flipbook", closure=True),
        SlotMapping("MouthFlipbook", "Flipbook", switch_slot="Use Flipbook", closure=True),
        SlotMapping("FB_MouthFlipbookTexture", "Flipbook", switch_slot="Use Second UV", closure=True),
    )

    scalars=(
        SlotMapping("SubImages"),
        SlotMapping("SubUV_Frames", "SubImages"),
        SlotMapping("FB_MouthRowCount", "SubImages"),
        SlotMapping("FB_MouthColumnCount", "SubImages"),
        SlotMapping("Flipbook X"),
        SlotMapping("FB_MouthUVOffsetX", "Flipbook X"),
        SlotMapping("Flipbook Y"),
        SlotMapping("FB_MouthUVOffsetY", "Flipbook Y"),
        SlotMapping("Flipbook Scale"),
        SlotMapping("MouthScale", "Flipbook Scale", value_func=lambda value: 1 / value),
        SlotMapping("FB_MouthUVScale", "Flipbook Scale"), # TODO: Add support for non-uniform scale
        SlotMapping("FB_MouthUVScaleX", "Flipbook Scale"),
        SlotMapping("FB_MouthUVScaleY", "Flipbook Scale"),
        SlotMapping("Use Second UV Channel", "Use Second UV"),

        SlotMapping("Affects Base Color"),
        SlotMapping("Multiply Flipbook Emissive"),

        SlotMapping("BumpOffset Intensity"),
        SlotMapping("Bump Height"),
    )

    colors=(
        SlotMapping("FlipbookTint"),
    )

    switches=(
        SlotMapping("Use Flipbook"),
        SlotMapping("Use Sub UV texture", "Use Flipbook"),
        SlotMapping("FB_UseMouth", "Use Flipbook"),
        SlotMapping("Use Second UV"),
        SlotMapping("UseUV2forMouth", "Use Second UV"),
        SlotMapping("FB_MouthUseUV2", "Use Second UV"),
        SlotMapping("Affects Base Color"),
        SlotMapping("Multiply Flipbook Emissive"),
        SlotMapping("useEmissiveforMouth", "Multiply Flipbook Emissive"),
    )


@registry.register
class EyelashMappings(MappingCollection):
    node_name="FPv4 Eyelash"
    type=ENodeType.NT_Advanced_FX

    textures=(
        SlotMapping("EyelashMask"),
    )

    scalars=(
        SlotMapping("EyelashMetallic"),
        SlotMapping("EyelashRoughness"),
        SlotMapping("EyelashSpec"),
    )

    colors=(
        SlotMapping("EyelashColor"),
        SlotMapping("EyelashVertexColorMaskChannel"),
    )

    switches=(
        SlotMapping("Use Eyelashes"),
        SlotMapping("UseEyelashes", "Use Eyelashes"),
    )


@registry.register
class GradientMappings(MappingCollection):
    node_name="FPv4 Gradient"
    type=ENodeType.NT_Advanced_FX
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return get_param(material_data.get("Switches"), "useGmapGradientLayers")


    textures=(
        SlotMapping("Layer Mask", alpha_slot="Layer Mask Alpha"),
        SlotMapping("Layer1_Gradient", closure=True),
        SlotMapping("Layer2_Gradient", closure=True),
        SlotMapping("Layer3_Gradient", closure=True),
        SlotMapping("Layer4_Gradient", closure=True),
        SlotMapping("Layer5_Gradient", closure=True),
    )

    switches=(
        SlotMapping("use Alpha Channel as mask", "Use Layer Mask Alpha"),
        SlotMapping("useGmapGradientLayers"),
        SlotMapping("useGmapGradientLayers", "Use Gmap Gradient Layers")
    )

    component_masks=(
        SlotMapping("GmapSkinCustomization_Channel"),
    )


@registry.register
class CustomColorMappings(MappingCollection):
    node_name="FPv4 CustomColor"
    type=ENodeType.NT_Advanced_FX

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), ["UseCustomColors", "Use Color Customization"])


    textures=(
        SlotMapping("TechArtMask"),
        SlotMapping("SkinFX_Mask", "TechArtMask"),
        SlotMapping("Custom Color Mask"),
        SlotMapping("TieDye_Pattern_Mask", "Custom Color Mask"),
        SlotMapping("Pattern Mask"),
        SlotMapping("CustomColor_mask", "Pattern Mask"),
    )

    colors=(
        SlotMapping("Base Color"),
        SlotMapping("PrimaryColor", "Base Color"),
        SlotMapping("TieDye_Color_1", "Base Color"),
        SlotMapping("Primary Color"),
        SlotMapping("AccentColor_01", "Primary Color"),
        SlotMapping("TieDye_Color_2", "Primary Color"),
        SlotMapping("Secondary Color"),
        SlotMapping("AccentColor_02", "Secondary Color"),
        SlotMapping("TieDye_Color_3", "Secondary Color"),
        SlotMapping("Tertiary Color"),
        SlotMapping("AccentColor_03", "Tertiary Color"),
        SlotMapping("Pattern Color"),
        SlotMapping("PatternColor", "Pattern Color"),
        SlotMapping("TieDye_Color_Base", "Pattern Color"),

        SlotMapping("Base Blend Color"),
        SlotMapping("Primary Blend Color"),
        SlotMapping("Secondary Blend Color"),
        SlotMapping("Tertiary Blend Color"),

        SlotMapping("Tech Art Mask Channel"),
        SlotMapping("PrimaryColorMask_Channel", "Tech Art Mask Channel"),
        SlotMapping("PrimaryMaskChannel"),
        SlotMapping("AccentColor_01_Channel", "PrimaryMaskChannel"),
        SlotMapping("SecondaryMaskChannel"),
        SlotMapping("AccentColor_02_Channel", "SecondaryMaskChannel"),
        SlotMapping("TertiaryMaskChannel"),
        SlotMapping("AccentColor_03_Channel", "TertiaryMaskChannel"),
        SlotMapping("Pattern Mask Channel"),
        SlotMapping("PatternMask_Channel", "Pattern Mask Channel"),
        SlotMapping("BaseColorValueChannel"),
        SlotMapping("BaseColorAdvancedBlendChannel"),
    )

    scalars=(
        SlotMapping("BaseColorValueBias"),
        SlotMapping("BaseColorValuePow"),
        SlotMapping("Pattern Strength"),
    )

    switches=(
        SlotMapping("UseAdvancedColorBlend"),
    )


@registry.register
class SkratchMappings(MappingCollection):
    node_name="FPv4 Skratch"
    type=ENodeType.NT_Advanced_FX
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return "TacticKale" in material_data.get("BaseMaterialPath")


    textures=(
        SlotMapping("CamoTex", closure=True),
        SlotMapping("CamuTex", "CamoTex", closure=True),
        SlotMapping("TattoosTex", closure=True),
        SlotMapping("TatoosTex", "TattoosTex", closure=True),
        SlotMapping("BannerTex", closure=True),
    )

    colors=(
        SlotMapping("Zone1Color"),
        SlotMapping("Zone2Color"),
        SlotMapping("Zone3Color"),
        SlotMapping("BannerColor"),
    )

    scalars=(
        SlotMapping("OverlayAdding"),
        SlotMapping("useCamo", "Use Camo", value_func=lambda value: int(value)),
        SlotMapping("isCamoUV2", "IsCamoUV2"),
        SlotMapping("CamoTiling"),
        SlotMapping("CamuTiling", "CamoTiling"),
        SlotMapping("CamoOffsetX"),
        SlotMapping("CamoOffsetY"),
        SlotMapping("CamoAdding"),
        SlotMapping("TC_UseSecondUVChannel(Tattoo)"),
        SlotMapping("TattooOpacity"),
        SlotMapping("TattooColorIntensity"),
        SlotMapping("Front_BannerIconSize"),
        SlotMapping("Front_BannerIconMask"),
        SlotMapping("Front_BannerPos"),
        SlotMapping("Back_BannerIconSizeX"),
        SlotMapping("Back_BannerIconSizeY"),
        SlotMapping("Back_BannerIconMaskX"),
        SlotMapping("Back_BannerIconMaskY"),
        SlotMapping("Back_BannerPosShirt"),
        SlotMapping("Back_BannerPosVest"),
    )


@registry.register
class SequinMappings(MappingCollection):
    node_name="FPv4 Sequin"
    type=ENodeType.NT_Advanced_FX
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), ["UseSequins", "UseSequin"]) and "M_DimeBlanket_Parent" not in material_data.get("BaseMaterialPath")

    textures=( # TODO: Santa 'Brina mappings
        SlotMapping("SequinOffset", default=DefaultTexture("T_SequinTile"), closure=True),
        SlotMapping("SequinOffest", "SequinOffset", closure=True),
        SlotMapping("SequinRoughness", default=DefaultTexture("T_SequinTile_roughness"), closure=True),
        SlotMapping("SequinNormal", default=DefaultTexture("T_SequinTile_N", False), closure=True),
        SlotMapping("StripeMask", default=DefaultTexture("T_SequinTile_StripesMask", False), closure=True), # TODO: Only used if UseStripes is on
        SlotMapping("SequinThinFilmColor", default=DefaultTexture("T_ThinFilm_Spectrum_COLOR"), closure=True), # TODO: Only used if UseThinFilmOnSequins is on
    )

    scalars=(
        SlotMapping("SequinTile"),
        SlotMapping("SequinRotationAngle"),
        SlotMapping("SequinThinFilmUVExponent"),
        SlotMapping("SequinThinFilmUVScale"),
        SlotMapping("SequinThinFilmUVOffset"),
        SlotMapping("SequinThinFilmStrength_Basecolor"),
        SlotMapping("SequinThinFilmStrength_Emissive"),
        SlotMapping("SequinFresnel"),
        SlotMapping("SequinColorOffsetMin"),
        SlotMapping("SequinColorOffsetMax"),
        SlotMapping("SequinBrightness"),
        SlotMapping("SequinEmissiveIntensity"),
        SlotMapping("Sequin_MinRoughness"),
        SlotMapping("Sequin_MaxRoughness"),
        SlotMapping("SequinBaseRoughnessBlendAmount"),
        SlotMapping("SequinMetalness"),
        SlotMapping("UseBaseMetalness"),
        SlotMapping("SequinSparkleSpeed"),
        SlotMapping("SequinDiamondTile"),
        SlotMapping("SparkleBrightness"),
        SlotMapping("SequinNormalIntensity"),
        SlotMapping("StripedColorBlend"),
        SlotMapping("StripedNormalBlend"),
    )

    colors=(
        SlotMapping("SequinMaskChannel"),
        SlotMapping("SequinChannel", "SequinMaskChannel"),
        SlotMapping("SequinFalloffColor01"),
        SlotMapping("SequinFalloffColor02"),
        SlotMapping("SparkleColor"),
    )

    switches=(
        SlotMapping("Use Sequins"),
        SlotMapping("UseSequins", "Use Sequins"),
        SlotMapping("MFSequin_UseThinFilmOnSequins", "UseThinFilmOnSequins"),
        SlotMapping("MFSequin_UseBaseColor"),
        SlotMapping("UseBaseRoughness"),
        SlotMapping("MFSequin_UseBaseRoughness", "UseBaseRoughness"),
        SlotMapping("UseBaseNormal"),
        SlotMapping("MFSequin_UseBaseNormal", "UseBaseNormal"),
        SlotMapping("UseStripes"),
    )

# TODO: Make trim and secondary overrides for regular?
@registry.register
class SequinTrimMappings(SequinMappings):
    node_name="FPv4 Sequin"
    type=ENodeType.NT_Advanced_FX
    order=20
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return get_param_multiple(material_data.get("Switches"), ["UseSequins", "UseSequin"]) \
            and "M_DimeBlanket_Parent" in material_data.get("BaseMaterialPath")


    textures=SequinMappings.textures + (
        SlotMapping("SequinOffset_Main", "SequinOffset", closure=True),
        SlotMapping("SequinOffest_Main", "SequinOffset", closure=True),
        SlotMapping("SequinRoughness_Main", "SequinRoughness", closure=True),
        SlotMapping("SequinNormal_,Main", "SequinNormal", closure=True),
        SlotMapping("SequinThinFilm_Trim", "SequinThinFilmColor", closure=True),
    )

    scalars=SequinMappings.scalars + (
        SlotMapping("SequinRotationAngle_Main", "SequinRotationAngle"),
    )

    colors=SequinMappings.colors + (
        SlotMapping("Sequin_SecondaryChannel", "SequinMaskChannel"),
        SlotMapping("SequinFalloffColor01_Main", "SequinFalloffColor01"),
        SlotMapping("SequinFalloffColor02_Main", "SequinFalloffColor02"),
    )


@registry.register
class SequinSecondaryMappings(SequinMappings):
    node_name="FPv4 Sequin"
    type=ENodeType.NT_Advanced_FX
    order=20.1
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        secondary_sequins = get_param(material_data.get("Switches"), "UseSecondarySequin")
        return get_param_multiple(material_data.get("Switches"), ["UseSequins", "UseSequin"]) \
            and "M_DimeBlanket_Parent" in material_data.get("BaseMaterialPath") \
            and (secondary_sequins is None or secondary_sequins is True)


    textures=SequinMappings.textures + (
        SlotMapping("SequinOffset_Secondary", "SequinOffset", closure=True),
        SlotMapping("SequinOffest_Secondary", "SequinOffset", closure=True),
        SlotMapping("SequinRoughness_Secondary", "SequinRoughness", closure=True),
        SlotMapping("SequinNormal_,Secondary", "SequinNormal", closure=True),
        SlotMapping("SequinThinFilm_Secondary", "SequinThinFilmColor", closure=True),
    )

    scalars=SequinMappings.scalars + (
        SlotMapping("SequinRotationAngle_Secondary", "SequinRotationAngle"),
    )

    colors=SequinMappings.colors + (
        SlotMapping("Sequin_TrimChannel", "SequinMaskChannel"),
        SlotMapping("SequinFalloffColor01_Secondary", "SequinFalloffColor01"),
        SlotMapping("SequinFalloffColor02_Secondary", "SequinFalloffColor02"),
    )


@registry.register
class GmapMappings(MappingCollection):
    node_name="FPv4 Gmap Material" # TODO: Rename to "Gmap" or "GMap Color"?
    type=ENodeType.NT_Advanced_FX

    @classmethod
    def meets_criteria(self, material_data):
        return get_param(material_data.get("Switches"), "Use Engine Colorized GMap")


    textures=(
        SlotMapping("Color Mask 1"),
        SlotMapping("Color Mask 2"),
        SlotMapping("Color Mask 3"),
        SlotMapping("ColorVariety/Scratch/Dirt Mask"),
    )

    colors=(
        SlotMapping("Base Color: Color A"),
        SlotMapping("Base Color: Color B"),
        SlotMapping("Base Color: Color C"),
        SlotMapping("Color Mask 1-R: Color A"),
        SlotMapping("Color Mask 1-R: Color B"),
        SlotMapping("Color Mask 1-R: Color C"),
        SlotMapping("Color Mask 1-G: Color A"),
        SlotMapping("Color Mask 1-G: Color B"),
        SlotMapping("Color Mask 1-G: Color C"),
        SlotMapping("Color Mask 1-B: Color A"),
        SlotMapping("Color Mask 1-B: Color B"),
        SlotMapping("Color Mask 1-B: Color C"),
        SlotMapping("Color Mask 2-R: Color A"),
        SlotMapping("Color Mask 2-R: Color B"),
        SlotMapping("Color Mask 2-R: Color C"),
        SlotMapping("Color Mask 2-G: Color A"),
        SlotMapping("Color Mask 2-G: Color B"),
        SlotMapping("Color Mask 2-G: Color C"),
        SlotMapping("Color Mask 2-B: Color A"),
        SlotMapping("Color Mask 2-B: Color B"),
        SlotMapping("Color Mask 2-B: Color C"),
        SlotMapping("Color Mask 3-R: Color A"),
        SlotMapping("Color Mask 3-R: Color B"),
        SlotMapping("Color Mask 3-R: Color C"),
        SlotMapping("Color Mask 3-G: Color A"),
        SlotMapping("Color Mask 3-G: Color B"),
        SlotMapping("Color Mask 3-G: Color C"),
        SlotMapping("Color Mask 3-B: Color A"),
        SlotMapping("Color Mask 3-B: Color B"),
        SlotMapping("Color Mask 3-B: Color C"),
        SlotMapping("Color Variety Mask: Color A"),
        SlotMapping("Color Variety Mask: Color B"),
        SlotMapping("Color Variety Mask: Color C"),
        SlotMapping("Scratch Color A"),
        SlotMapping("Scratch Color B"),
        SlotMapping("Dirt Color A"),
        SlotMapping("Dirt Color B"),
    )

    scalars=(
        SlotMapping("Color Variety Mask: Opacity"),
    )

    switches=(
        SlotMapping("Use Diffuse as Base Color"),
        SlotMapping("Uses 2+ Color Masks", "Mask 2"),
        SlotMapping("Uses 3 Color Masks", "Mask 3"),
        SlotMapping("Uses ColorVariety/Scratch/Dirt Mask", "ColorVariety/Scratch/Dirt Mask"),
    )


@registry.register
class SuperheroMappings(MappingCollection):
    node_name="FPv4 Superhero"
    type=ENodeType.NT_Advanced_FX
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        base_material_path = material_data.get("BaseMaterialPath")
        return "Elastic_Master" in base_material_path and "_Head_" not in base_material_path


    textures=(
        SlotMapping("Pattern"),
        SlotMapping("PrimaryNormal", switch_slot="Use Primary Normal"),
        SlotMapping("SecondaryNormal", switch_slot="Use Secondary Normal"),
        SlotMapping("ZE_SecondaryNormal", "SecondaryNormal", switch_slot="Use Secondary Normal"),
        SlotMapping("Sticker", switch_slot="Use Sticker", closure=True)
    )

    colors=(
        SlotMapping("PrimaryColor"),
        SlotMapping("SecondaryColor"),
        SlotMapping("AccessoryColor"),
        SlotMapping("PrimaryMaterial"),
        SlotMapping("SecondaryMaterial"),
        SlotMapping("AccessoryMaterial"),
        SlotMapping("Sticker MSRE"),
        SlotMapping("Cloth Fuzz Tint"),
    )

    vectors=(
        SlotMapping("StickerPosition", default=(0.02, -0.21, 0.0, 0.0)),
        SlotMapping("StickerScale", default=(-0.06, -0.06, 0.0, 0.0)),
        SlotMapping("BackStickerPosition", default=(0.32, 0.4, 0.0, 0.0)),
        SlotMapping("BackStickerScale", default=(0.07, 0.07, 0.0, 0.0)),
    )

    scalars=(
        SlotMapping("PrimaryCloth"),
        SlotMapping("SecondaryCloth"),
        SlotMapping("ElasticStickerMult"),
        SlotMapping("UseAccessoryMaterial"),
        SlotMapping("Fuzz Tiling"),
        SlotMapping("Fuzz Exponent"),
        SlotMapping("Fuzz Fresnel Blend"),
        SlotMapping("Cloth Base Color Intensity"),
        SlotMapping("Cloth Roughness"),
    )


@registry.register
class GalaxyMappings(MappingCollection):
    node_name="FPv4 Galaxy"
    type=ENodeType.NT_Advanced_FX
    node_spacing=700

    @classmethod
    def meets_criteria(self, material_data):
        return get_param(material_data.get("Switches"), "Use Galaxy")


    textures=(
        SlotMapping("GalaxyTexture", default=DefaultTexture("T_FN_Nebula"), closure=True),
        SlotMapping("Stars", default=DefaultTexture("Celestial_T_Stars", False), closure=True),
    )

    colors=(
        SlotMapping("Galaxy Channel"),
        SlotMapping("Star Brightness"),
        SlotMapping("Star Brightness 1"),
        SlotMapping("Galaxy Warp Mask Channel"),
        SlotMapping("GalaxyEF"),
        SlotMapping("EdgeFresnel"),
    )

    scalars=(
        SlotMapping("Galaxy_GlobalBrightness"),
        SlotMapping("Star Saturation"),
        SlotMapping("Galaxy Roughness"),
        SlotMapping("Galaxy Specular"),
        SlotMapping("WarpIntensity"),
        SlotMapping("Galaxy Tiling"),
        SlotMapping("GalaxyAxisFade"),
        SlotMapping("Galaxy Rotation Speed"),
        SlotMapping("Galaxy Rotation Speed 1"),
        SlotMapping("Galaxy Rotation Speed 2"),
        SlotMapping("Small Star Tiling"),
        SlotMapping("Small Star Rotation Speed"),
        SlotMapping("Bright Star Tiling"),
        SlotMapping("Galaxy_FresEx"),
        SlotMapping("GalaxyFlameFresnel Intensite"),
        SlotMapping("AccentRimBrightness"),
    )

    switches=(
        SlotMapping("Use Galaxy"),
    )


# Hair and Fur should be at the very end of the chain
@registry.register
class HairMappings(MappingCollection):
    node_name="FPv4 Hair"
    type=ENodeType.NT_Advanced_FX
    order=98

    @classmethod
    def meets_criteria(self, material_data):
        base_material_path = material_data.get("BaseMaterialPath")
        return "M_HairParent_2023" in base_material_path or (get_param(material_data.get("Textures"), "Hair Mask") is not None and "fur" not in base_material_path.lower())


    textures=(
        SlotMapping("Hair Mask"),
        SlotMapping("AnisotropicTangentWeight", alpha_slot="AnisotropicTangentWeight Alpha"),
        SlotMapping("Strands Normal"),
    )

    colors=(
        SlotMapping("Hair_Color_Variation"),
        SlotMapping("Paint_Hair_Color_Darkness"),
        SlotMapping("Paint_Hair_Color_Brightness"),
    )

    scalars=(
        SlotMapping("AmbientOcclusion_Black"),
        SlotMapping("BaseColor_Fresnel_Brightness"),
        SlotMapping("Basecolor_Fresnel_Exponent"),
        SlotMapping("Fresnel_Brightness_Multiple"),

        SlotMapping("Paint_Hair_Contrast"),
        SlotMapping("Gmap_intensity"),

        SlotMapping("Specular_POWER"),
        SlotMapping("Specular _POWER", "Specular_POWER"),
        SlotMapping("Hair_Specular_MIN"),
        SlotMapping("Hair_Specular_MAX"),
        SlotMapping("Hair_Metallic"),
        SlotMapping("Roughness_power"),
        SlotMapping("Roughness Min"),
        SlotMapping("Roughness Max"),
        SlotMapping("Roughness_Noise_Tiling"),
        SlotMapping("Hair_Noise_Roughness_Min"),

        SlotMapping("Emissive_Brightness"),

        SlotMapping("AnisotropyMaxWeight"),
        SlotMapping("Hair_Anisotropy_Min"),
        SlotMapping("Hair_Anisotropy_Max"),
        SlotMapping("Scraggle"),

        SlotMapping("Hair_Mesh_Normal_Flatness"),
        SlotMapping("Paint_Hair_Normal_Flatness"),
    )

    switches=(
        SlotMapping("UseAnisotropicShading"),
    )


@registry.register
class FurMappings(MappingCollection):
    node_name="FPv4 Fur"
    type=ENodeType.NT_Advanced_FX
    order=99

    @classmethod
    def meets_criteria(self, material_data):
        base_material_path = material_data.get("BaseMaterialPath")
        return "M_Companion_Fur_Parent_2025" in base_material_path or "furparent" in base_material_path.lower()


    textures=(
        SlotMapping("Strand Map"),
        SlotMapping("Hair Mask Height", "Strand Map"),
        SlotMapping("AnisotropicTangentWeight", alpha_slot="AnisotropicTangentWeight Alpha"),
        SlotMapping("Strands Normal"),
    )

    colors=(
        SlotMapping("Fur_Color_Darkness"),
        SlotMapping("Paint_Hair_Color_Darkness", "Fur_Color_Darkness"),
        SlotMapping("Fur_Color_Brightness"),
        SlotMapping("Paint_Hair_Color_Brightness", "Fur_Color_Brightness"),
    )

    scalars=(
        SlotMapping("AmbientOcclusion_Black"),
        SlotMapping("BaseColor_Fresnel_Brightness"),
        SlotMapping("Basecolor_Fresnel_Exponent"),
        SlotMapping("Fresnel_Brightness_Multiple"),

        SlotMapping("Fur_Contrast"),
        SlotMapping("Paint_Hair_Contrast", "Fur_Contrast"),
        SlotMapping("Gmap_Intensity"),

        SlotMapping("Specular_POWER"),
        SlotMapping("Specular _POWER", "Specular_POWER"),
        SlotMapping("Fur_Specular_Min"),
        SlotMapping("Hair_Specular_Min", "Fur_Specular_Min"),
        SlotMapping("Fur_Specular_Max"),
        SlotMapping("Hair_Specular_Max", "Fur_Specular_Max"),
        SlotMapping("Fur_Metallic"),
        SlotMapping("Metallic_Min"),
        SlotMapping("Hair_Metallic_Min", "Metallic_Min"),
        SlotMapping("Metallic_Max"),
        SlotMapping("Hair_Metallic_Max", "Metallic_Max"),
        SlotMapping("Roughness_power"),
        SlotMapping("Roughness_Min"),
        SlotMapping("Roughness Min", "Roughness_Min"),
        SlotMapping("Roughness_Max"),
        SlotMapping("Roughness Max", "Roughness_Max"),
        SlotMapping("Roughness_Noise_Tiling"),
        SlotMapping("Scraggle_NoiseZ_Tiling", "Roughness_Noise_Tiling"),
        SlotMapping("Roughness_Noise_Min"),
        SlotMapping("Hair_Noise_Roughness_Min", "Roughness_Noise_Min"),

        SlotMapping("Emissive_Fur_Brightness"),

        SlotMapping("AnisotropyMaxWeight"),
        SlotMapping("Fur_Anisotropy_Min"),
        SlotMapping("Hair_Anisotropy0_Min", "Fur_Anisotropy_Min"),
        SlotMapping("Fur_Anisotropy_Max"),
        SlotMapping("Hair_Anisotropy0_Max", "Fur_Anisotropy_Max"),
        SlotMapping("Scraggle_Strength"),
        SlotMapping("Scraggle", "Scraggle_Strength"),

        SlotMapping("Mesh_Normal_Flatness"),
        SlotMapping("Hair_Mesh_Normal_Flatness", "Mesh_Normal_Flatness"),
        SlotMapping("Fur_Normal_Flatness"),
        SlotMapping("Paint_Hair_Normal_Flatness", "Fur_Normal_Flatness"),
    )

    switches=(
        SlotMapping("UseAnisotropicShading"),
    )

# End advanced FX groups