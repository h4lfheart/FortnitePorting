from ..enums import *

layer_switch_names = [
    "Use 2 Layers", "Use 3 Layers", "Use 4 Layers", "Use 5 Layers", "Use 6 Layers", "Use 7 Layers",
    "Use 2 Materials", "Use 3 Materials", "Use 4 Materials", "Use 5 Materials", "Use 6 Materials", "Use 7 Materials",
    "Use_Multiple_Material_Textures"
]

extra_layer_names = [
    "Diffuse_Texture_2", "SpecularMasks_2", "Normals_Texture_2", "Emissive_Texture_2",
    "Diffuse_Texture_3", "SpecularMasks_3", "Normals_Texture_3", "Emissive_Texture_3",
    "Diffuse_Texture_4", "SpecularMasks_4", "Normals_Texture_4", "Emissive_Texture_4",
    "Diffuse_Texture_5", "SpecularMasks_5", "Normals_Texture_5", "Emissive_Texture_5",
    "Diffuse_Texture_6", "SpecularMasks_6", "Normals_Texture_6", "Emissive_Texture_6"
]

toon_texture_names = ["LitDiffuse", "ShadedDiffuse", "Color_Lit_Map", "Color_Shaded_Map"]

toon_vector_names = ["Color_Lit", "Color_Shaded"]

emissive_toggle_names = [
    "Emissive",
    "UseBasicEmissive",
    "UseAdvancedEmissive",
    "UseAnimatedEmissive",
    "Use Emissive"
]

emissive_crop_vector_names = [
    "EmissiveUVs_RG_UpperLeftCorner_BA_LowerRightCorner",
    "Emissive Texture UVs RG_TopLeft BA_BottomRight",
    "Emissive 2 UV Positioning (RG)UpperLeft (BA)LowerRight",
    "EmissiveUVPositioning (RG)UpperLeft (BA)LowerRight"
]

emissive_crop_switch_names = [
    "CroppedEmissive",
    "Manipulate Emissive Uvs"
]

texture_ignore_names = ["DefaultTexture"]

vertex_crunch_names = ["MI_VertexCrunch", "M_VertexCrunch", "M_Invis"]

toon_outline_names = ["Outline", "Toon_Lines"]

glass_master_names = ["M_MED_Glass_Master"]

lite_shader_types = [EExportType.PROP, EExportType.PREFAB, EExportType.WORLD]