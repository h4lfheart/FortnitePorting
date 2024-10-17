#include "FortnitePorting/Public/Processing/MaterialMappings.h"

const FMappingCollection FMaterialMappings::Default {
	.Textures = {
		FSlotMapping("Diffuse"),
		FSlotMapping("D", "Diffuse"),
		FSlotMapping("Base Color", "Diffuse"),
		FSlotMapping("BaseColor", "Diffuse"),
		FSlotMapping("Concrete", "Diffuse"),
		FSlotMapping("Trunk_BaseColor", "Diffuse"),
		FSlotMapping("Diffuse Top", "Diffuse"),
		FSlotMapping("BaseColor_Trunk", "Diffuse"),
		FSlotMapping("CliffTexture", "Diffuse"),
		FSlotMapping("PM_Diffuse", "Diffuse"),
		
		
		FSlotMapping("Background Diffuse"),
		FSlotMapping("BG Diffuse Texture", "Background Diffuse"),

		FSlotMapping("M"),
		FSlotMapping("Mask", "M"),

		FSlotMapping("SpecularMasks"),
		FSlotMapping("S", "SpecularMasks"),
		FSlotMapping("SRM", "SpecularMasks"),
		FSlotMapping("Specular Mask", "SpecularMasks"),
		FSlotMapping("SpecularMask", "SpecularMasks"),
		FSlotMapping("Concrete_SpecMask", "SpecularMasks"),
		FSlotMapping("Trunk_Specular", "SpecularMasks"),
		FSlotMapping("Specular Top", "SpecularMasks"),
		FSlotMapping("SMR_Trunk", "SpecularMasks"),
		FSlotMapping("Cliff Spec Texture", "SpecularMasks"),

		FSlotMapping("Normals"),
		FSlotMapping("N", "Normals"),
		FSlotMapping("Normal", "Normals"),
	  	FSlotMapping("NormalMap", "Normals"),
	  	FSlotMapping("ConcreteTextureNormal", "Normals"),
	  	FSlotMapping("Trunk_Normal", "Normals"),
	  	FSlotMapping("Normals Top", "Normals"),
	  	FSlotMapping("Normal_Trunk", "Normals"),
	  	FSlotMapping("CliffNormal", "Normals"),
	  	FSlotMapping("PM_Normals", "Normals"),
		
		FSlotMapping("MaskTexture"),
		FSlotMapping("OpacityMask", "MaskTexture"),
	},
	
	.Scalars = {
		FSlotMapping("RoughnessMin", "Roughness Min"),
		FSlotMapping("SpecRoughnessMin", "Roughness Min"),
		FSlotMapping("RawRoughnessMin", "Roughness Min"),
		FSlotMapping("Rough Min", "Roughness Min"),
		
		FSlotMapping("RoughnessMax", "Roughness Max"),
		FSlotMapping("SpecRoughnessMax", "Roughness Max"),
		FSlotMapping("RawRoughnessMax", "Roughness Max"),
		FSlotMapping("Rough Max", "Roughness Max"),
	},
	
	.Switches = {
		FSlotMapping("SwizzleRoughnessToGreen")
	}
};

const FMappingCollection FMaterialMappings::Layer {
	.Textures = {
		FSlotMapping("Diffuse"),
		FSlotMapping("SpecularMasks"),
	    FSlotMapping("Normals"),
	    FSlotMapping("EmissiveTexture"),
	    FSlotMapping("MaskTexture"),
	    FSlotMapping("Background Diffuse"),
		
	    FSlotMapping("Diffuse_Texture_2"),
	    FSlotMapping("SpecularMasks_2"),
	    FSlotMapping("Normals_Texture_2"),
	    FSlotMapping("Emissive_Texture_2"),
	    FSlotMapping("MaskTexture_2"),
	    FSlotMapping("Background Diffuse 2"),
		
	    FSlotMapping("Diffuse_Texture_3"),
	    FSlotMapping("SpecularMasks_3"),
	    FSlotMapping("Normals_Texture_3"),
	    FSlotMapping("Emissive_Texture_3"),
	    FSlotMapping("MaskTexture_3"),
	    FSlotMapping("Background Diffuse 3"),
		
	    FSlotMapping("Diffuse_Texture_4"),
	    FSlotMapping("SpecularMasks_4"),
	    FSlotMapping("Normals_Texture_4"),
	    FSlotMapping("Emissive_Texture_4"),
	    FSlotMapping("MaskTexture_4"),
	    FSlotMapping("Background Diffuse 4"),
		
	    FSlotMapping("Diffuse_Texture_5"),
	    FSlotMapping("SpecularMasks_5"),
	    FSlotMapping("Normals_Texture_5"),
	    FSlotMapping("Emissive_Texture_5"),
	    FSlotMapping("MaskTexture_5"),
	    FSlotMapping("Background Diffuse 5"),
		
	    FSlotMapping("Diffuse_Texture_6"),
	    FSlotMapping("SpecularMasks_6"),
	    FSlotMapping("Normals_Texture_6"),
	    FSlotMapping("Emissive_Texture_6"),
	    FSlotMapping("MaskTexture_6"),
	    FSlotMapping("Background Diffuse 6"),
	},
};