class MappingCollection:
    def __init__(self, textures=(), scalars=(), vectors=(), switches=(), component_masks=()):
        self.textures = textures
        self.scalars = scalars
        self.vectors = vectors
        self.switches = switches
        self.component_masks = component_masks


class SlotMapping:
    def __init__(self, name, slot=None, alpha_slot=None, switch_slot=None, value_func=None, coords="UV0"):
        self.name = name
        self.slot = name if slot is None else slot
        self.alpha_slot = alpha_slot
        self.switch_slot = switch_slot
        self.value_func = value_func
        self.coords = coords

default_mappings = MappingCollection(
    textures=[
        SlotMapping("Diffuse"),
        SlotMapping("D", "Diffuse"),
        SlotMapping("Base Color", "Diffuse"),
        SlotMapping("Concrete", "Diffuse"),
        SlotMapping("Trunk_BaseColor", "Diffuse"),
        SlotMapping("Diffuse Top", "Diffuse"),
        SlotMapping("BaseColor_Trunk", "Diffuse"),
        SlotMapping("CliffTexture", "Diffuse"),
        SlotMapping("PM_Diffuse", "Diffuse"),

        SlotMapping("Background Diffuse", alpha_slot="Background Diffuse Alpha"),
        SlotMapping("BG Diffuse Texture", "Background Diffuse", alpha_slot="Background Diffuse Alpha"),

        SlotMapping("M"),
        SlotMapping("Mask", "M"),

        SlotMapping("SpecularMasks"),
        SlotMapping("S", "SpecularMasks"),
        SlotMapping("SRM", "SpecularMasks"),
        SlotMapping("Specular Mask", "SpecularMasks"),
        SlotMapping("Concrete_SpecMask", "SpecularMasks"),
        SlotMapping("Trunk_Specular", "SpecularMasks"),
        SlotMapping("Specular Top", "SpecularMasks"),
        SlotMapping("SMR_Trunk", "SpecularMasks"),
        SlotMapping("Cliff Spec Texture", "SpecularMasks"),
        SlotMapping("PM_SpecularMasks", "SpecularMasks"),

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

        SlotMapping("Emissive", "Emission"),
        SlotMapping("EmissiveTexture", "Emission"),
        SlotMapping("L1_Emissive", "Emission", coords="UV2"),
        SlotMapping("PM_Emissive", "Emission"),

        SlotMapping("MaskTexture"),
        SlotMapping("OpacityMask", "MaskTexture"),

        SlotMapping("SkinFX_Mask"),
        SlotMapping("SkinFX Mask", "SkinFX_Mask"),
        SlotMapping("TechArtMask", "SkinFX_Mask"),
        
        SlotMapping("Thin Film Texture")
    ],
    scalars=[
        SlotMapping("RoughnessMin", "Roughness Min"),
        SlotMapping("SpecRoughnessMin", "Roughness Min"),
        SlotMapping("RawRoughnessMin", "Roughness Min"),
        SlotMapping("Rough Min", "Roughness Min"),
        SlotMapping("RoughnessMax", "Roughness Max"),
        SlotMapping("SpecRoughnessMax", "Roughness Max"),
        SlotMapping("RawRoughnessMax", "Roughness Max"),
        SlotMapping("Rough Max", "Roughness Max"),
        SlotMapping("emissive mult", "Emission Strength"),
        SlotMapping("DayMult", "Emission Strength"),

        SlotMapping("ThinFilm_Intensity"),
        SlotMapping("ThinFilmIntensity", "ThinFilm_Intensity"),
        SlotMapping("ThinFilm_RoughnessScale"),
        SlotMapping("ThinFilmRoughnessScale", "ThinFilm_RoughnessScale"),
        SlotMapping("ThinFilm_Exponent"),
        SlotMapping("ThinFilmExponent", "ThinFilm_Exponent"),
        SlotMapping("ThinFilm_Offset"),
        SlotMapping("ThinFilmOffset", "ThinFilm_Offset"),
        SlotMapping("ThinFilm_Scale"),
        SlotMapping("ThinFilmScale", "ThinFilm_Scale"),
        SlotMapping("ThinFilm_Warp"),
        SlotMapping("ThinFilmWarp", "ThinFilm_Warp")
    ],
    vectors=[
        SlotMapping("Skin Boost Color And Exponent", "Skin Color", alpha_slot="Skin Boost"),
        SlotMapping("SkinTint", "Skin Color", alpha_slot="Skin Boost"),
        SlotMapping("EmissiveMultiplier", "Emission Multiplier"),
        SlotMapping("Emissive Multiplier", "Emission Multiplier"),
        SlotMapping("Emissive Color", "Emission Color", switch_slot="Use Emission Color"),
        SlotMapping("Emissive", "Emission Color", switch_slot="Use Emission Color"),
        
        SlotMapping("ThinFilm_Channel"),
        SlotMapping("ThinFilmMaskChannel", "ThinFilm_Channel")
    ],
    switches=[
        SlotMapping("SwizzleRoughnessToGreen")
    ],
    component_masks=[
        SlotMapping("ThinFilm_Channel"),
        SlotMapping("ThinFilmMaskChannel", "ThinFilm_Channel")
    ]
)

layer_mappings = MappingCollection(
    textures=[
        SlotMapping("Diffuse", alpha_slot="MaskTexture"),
        SlotMapping("SpecularMasks"),
        SlotMapping("Normals"),
        SlotMapping("EmissiveTexture"),
        SlotMapping("MaskTexture"),
        SlotMapping("Background Diffuse", alpha_slot="Background Diffuse Alpha"),

        SlotMapping("Diffuse_Texture_2", alpha_slot="MaskTexture_2"),
        SlotMapping("SpecularMasks_2"),
        SlotMapping("Normals_Texture_2"),
        SlotMapping("Emissive_Texture_2"),
        SlotMapping("MaskTexture_2"),
        SlotMapping("Background Diffuse 2", alpha_slot="Background Diffuse Alpha 2"),

        SlotMapping("Diffuse_Texture_3", alpha_slot="MaskTexture_3"),
        SlotMapping("SpecularMasks_3"),
        SlotMapping("Normals_Texture_3"),
        SlotMapping("Emissive_Texture_3"),
        SlotMapping("MaskTexture_3"),
        SlotMapping("Background Diffuse 3", alpha_slot="Background Diffuse Alpha 3"),

        SlotMapping("Diffuse_Texture_4", alpha_slot="MaskTexture_4"),
        SlotMapping("SpecularMasks_4"),
        SlotMapping("Normals_Texture_4"),
        SlotMapping("Emissive_Texture_4"),
        SlotMapping("MaskTexture_4"),
        SlotMapping("Background Diffuse 4", alpha_slot="Background Diffuse Alpha 4"),

        SlotMapping("Diffuse_Texture_5", alpha_slot="MaskTexture_5"),
        SlotMapping("SpecularMasks_5"),
        SlotMapping("Normals_Texture_5"),
        SlotMapping("Emissive_Texture_5"),
        SlotMapping("MaskTexture_5"),
        SlotMapping("Background Diffuse 5", alpha_slot="Background Diffuse Alpha 5"),

        SlotMapping("Diffuse_Texture_6", alpha_slot="MaskTexture_6"),
        SlotMapping("SpecularMasks_6"),
        SlotMapping("Normals_Texture_6"),
        SlotMapping("Emissive_Texture_6"),
        SlotMapping("MaskTexture_6"),
        SlotMapping("Background Diffuse 6", alpha_slot="Background Diffuse Alpha 6"),
    ]
)

toon_mappings = MappingCollection(
    textures=[
        SlotMapping("LitDiffuse"),
        SlotMapping("Color_Lit_Map", "LitDiffuse"),
        SlotMapping("ShadedDiffuse"),
        SlotMapping("Color_Shaded_Map", "ShadedDiffuse"),
        SlotMapping("DistanceField_InkLines"),
        SlotMapping("DFL_Map", "DistanceField_InkLines"),
        SlotMapping("InkLineColor_Texture"),
        SlotMapping("SSC_Texture"),
        SlotMapping("STM_Map", "SSC_Texture"),
        SlotMapping("STT_Map"),
        SlotMapping("Normals"),
        SlotMapping("Normal_Map", "Normals")
    ],
    scalars=[
        SlotMapping("ShadedColorDarkening"),
        SlotMapping("FakeNormalBlend_Amt"),
        SlotMapping("VertexBakedNormal_Blend", "FakeNormalBlend_Amt"),
        SlotMapping("PBR_Shading", "Use PBR Shading", value_func=lambda value: int(value))
    ],
    vectors=[
        SlotMapping("InkLineColor", "InkLineColor_Texture"),
        SlotMapping("Color_Lit", "LitDiffuse"),
        SlotMapping("Color_Shaded", "ShadedDiffuse"),
    ]
)

valet_mappings = MappingCollection(
    textures=[
        SlotMapping("Diffuse"),
        SlotMapping("Mask", alpha_slot="Mask Alpha"),
        SlotMapping("Decal", alpha_slot="Decal Alpha", coords="UV1"),
        SlotMapping("Normal"),
        SlotMapping("Specular Mask"),
        SlotMapping("Scratch/Grime/EMPTY"),
    ],
    scalars=[
        SlotMapping("Scratch Intensity"),
        SlotMapping("Grime Intensity"),
        SlotMapping("Grime Spec"),
        SlotMapping("Grime Roughness"),

        SlotMapping("Layer 01 Specular"),
        SlotMapping("Layer 01 Metalness"),
        SlotMapping("Layer 01 Roughness Min"),
        SlotMapping("Layer 01 Roughness Max"),
        SlotMapping("Layer 01 Clearcoat"),
        SlotMapping("Layer 01 Clearcoat Roughness Min"),
        SlotMapping("Layer 01 Clearcoat Roughness Max"),

        SlotMapping("Layer 02 Specular"),
        SlotMapping("Layer 02 Metalness"),
        SlotMapping("Layer 02 Roughness Min"),
        SlotMapping("Layer 02 Roughness Max"),
        SlotMapping("Layer 02 Clearcoat"),
        SlotMapping("Layer 02 Clearcoat Roughness Min"),
        SlotMapping("Layer 02 Clearcoat Roughness Max"),

        SlotMapping("Layer 03 Specular"),
        SlotMapping("Layer 03 Metalness"),
        SlotMapping("Layer 03 Roughness Min"),
        SlotMapping("Layer 03 Roughness Max"),
        SlotMapping("Layer 03 Clearcoat"),
        SlotMapping("Layer 03 Clearcoat Roughness Min"),
        SlotMapping("Layer 03 Clearcoat Roughness Max"),

        SlotMapping("Layer 04 Specular"),
        SlotMapping("Layer 04 Metalness"),
        SlotMapping("Layer 04 Roughness Min"),
        SlotMapping("Layer 04 Roughness Max"),
        SlotMapping("Layer 04 Clearcoat"),
        SlotMapping("Layer 04 Clearcoat Roughness Min"),
        SlotMapping("Layer 04 Clearcoat Roughness Max"),
    ],
    vectors=[
        SlotMapping("Scratch Tint"),
        SlotMapping("Grime Tint"),

        SlotMapping("Layer 01 Color"),
        SlotMapping("Layer 02 Color"),
        SlotMapping("Layer 03 Color"),
        SlotMapping("Layer 04 Color"),
    ]
)

glass_mappings = MappingCollection(
    textures=[
        SlotMapping("Color_DarkTint"),
        SlotMapping("Diffuse", "Color"),
        SlotMapping("Diffuse Texture", "Color"),
        SlotMapping("Normals"),
        SlotMapping("BakedNormal", "Normals"),
        SlotMapping("Diffuse Texture with Alpha Mask", "Color", alpha_slot="Mask")
    ],
    scalars=[
        SlotMapping("Specular"),
        SlotMapping("GlassSpecular", "Specular"),
        SlotMapping("Metallic"),
        SlotMapping("GlassMetallic", "Metallic"),
        SlotMapping("Roughness"),
        SlotMapping("GlassRoughness", "Roughness"),
        SlotMapping("Window Tint Amount", "Tint Amount"),
        SlotMapping("Exponent"),
        SlotMapping("Fresnel Exponent", "Exponent"),
        SlotMapping("FresnelExponentTransparency", "Exponent"),
        SlotMapping("Inner Transparency"),
        SlotMapping("InnerTransparency", "Inner Transparency"),
        SlotMapping("Fresnel Inner Transparency", "Inner Transparency"),
        SlotMapping("Inner Transparency Max Tint"),
        SlotMapping("Fresnel Inner Transparency Max Tint", "Inner Transparency Max Tint"),
        SlotMapping("Outer Transparency"),
        SlotMapping("OuterTransparency", "Outer Transparency"),
        SlotMapping("Fresnel Outer Transparency", "Outer Transparency"),
        SlotMapping("Glass thickness", "Thickness"),
        SlotMapping("GlassThickness", "Thickness"),
        SlotMapping("Alpha Channel Mask Opacity", "Mask Opacity")
    ],
    vectors=[
        SlotMapping("ColorFront", "Color"),
        SlotMapping("Base Color", "Color"),
    ]
)

trunk_mappings = MappingCollection(
    textures=[
        SlotMapping("Trunk_BaseColor", "Diffuse"),
        SlotMapping("Trunk_Specular", "SpecularMasks"),
        SlotMapping("Trunk_Normal", "Normals"),
        SlotMapping("BaseColor_Trunk", "Diffuse"),
        SlotMapping("SMR_Trunk", "SpecularMasks"),
        SlotMapping("Normal_Trunk", "Normals"),
    ]
)

foliage_mappings = MappingCollection(
    textures=[
        SlotMapping("Diffuse"),
        SlotMapping("Normals"),
        SlotMapping("MaskTexture"),
    ],
    scalars=[
        SlotMapping("Roughness Leafs", "Roughness"),
        SlotMapping("Specular_Leafs", "Specular")
    ],
    vectors=[
        SlotMapping("Color1_Base"),
        SlotMapping("Color2_Lit"),
        SlotMapping("Color3_Shadows")
    ]
)

gradient_mappings = MappingCollection(
    textures=[
        SlotMapping("Diffuse"),
        SlotMapping("Layer Mask", alpha_slot="Layer Mask Alpha"),
        SlotMapping("SkinFX_Mask"),
        SlotMapping("Layer1_Gradient"),
        SlotMapping("Layer2_Gradient"),
        SlotMapping("Layer3_Gradient"),
        SlotMapping("Layer4_Gradient"),
        SlotMapping("Layer5_Gradient"),
    ],
    switches=[
        SlotMapping("use Alpha Channel as mask", "Use Layer Mask Alpha")
    ],
    component_masks=[
        SlotMapping("GmapSkinCustomization_Channel")
    ]
)

bean_base_mappings = MappingCollection(
    textures=[
        SlotMapping("Body_Pattern", coords="UV1"),
    ],
    vectors=[
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
    ]
)

bean_costume_mappings = MappingCollection(
    textures=[
        SlotMapping("Metalness/Roughness/Specular/Albedo", "Metalness/Roughness/Specular", alpha_slot="Albedo"),
        SlotMapping("MaterialMasking"),
        SlotMapping("NormalMap"),
    ],
    vectors=[
        SlotMapping("Costume_MainColor"),
        SlotMapping("Costume_MainMaterialProps"),
        SlotMapping("Costume_Secondary_Color"),
        SlotMapping("Costume_SecondaryMaterialProps"),
        SlotMapping("Costume_AccentColor"),
        SlotMapping("Costume_AccentMaterialProps"),
    ]
)

bean_head_costume_mappings = MappingCollection(
    textures=[
        SlotMapping("Metalness/Roughness/Specular/Albedo", "Metalness/Roughness/Specular", alpha_slot="Albedo"),
        SlotMapping("MaterialMasking"),
        SlotMapping("NormalMap"),
    ],
    vectors=[
        SlotMapping("Head_Costume_MainColor", "Costume_MainColor"),
        SlotMapping("Head_Costume_MainMaterialProps", "Costume_MainMaterialProps"),
        SlotMapping("Head_Costume_Secondary_Color", "Costume_Secondary_Color"),
        SlotMapping("Head_Costume_SecondaryMaterialProps", "Costume_SecondaryMaterialProps"),
        SlotMapping("Head_Costume_AccentColor", "Costume_AccentColor"),
        SlotMapping("Head_Costume_AccentMaterialProps", "Costume_AccentMaterialProps"),
    ]
)