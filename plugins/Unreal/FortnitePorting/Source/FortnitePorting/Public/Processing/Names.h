#pragma once

class FNames
{
public:
	static inline TArray<FString> LayerSwitchNames
	{
		"Use 2 Layers", "Use 3 Layers", "Use 4 Layers", "Use 5 Layers", "Use 6 Layers", "Use 7 Layers",
		"Use 2 Materials", "Use 3 Materials", "Use 4 Materials", "Use 5 Materials", "Use 6 Materials", "Use 7 Materials",
		"Use_Multiple_Material_Textures"
	};

	static inline TArray<FString> LayerTextureNames
	{
		"Diffuse_Texture_2", "SpecularMasks_2", "Normals_Texture_2", "Emissive_Texture_2",
		 "Diffuse_Texture_3", "SpecularMasks_3", "Normals_Texture_3", "Emissive_Texture_3",
		 "Diffuse_Texture_4", "SpecularMasks_4", "Normals_Texture_4", "Emissive_Texture_4",
		 "Diffuse_Texture_5", "SpecularMasks_5", "Normals_Texture_5", "Emissive_Texture_5",
		 "Diffuse_Texture_6", "SpecularMasks_6", "Normals_Texture_6", "Emissive_Texture_6"
	};
};