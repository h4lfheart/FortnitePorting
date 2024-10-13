#pragma once
#include "ExportMaterial.generated.h"

USTRUCT()
struct FTextureParameter
{
	GENERATED_BODY()

	UPROPERTY()
	FString Name;
	
	UPROPERTY()
	FString Value;
	
	UPROPERTY()
	bool sRGB;
	
	UPROPERTY()
	TEnumAsByte<TextureCompressionSettings> CompressionSettings;
};

USTRUCT()
struct FScalarParameter
{
	GENERATED_BODY()

	UPROPERTY()
	FString Name;
	
	UPROPERTY()
	float Value;
};

USTRUCT()
struct FVectorParameter
{
	GENERATED_BODY()

	UPROPERTY()
	FString Name;
	
	UPROPERTY()
	FLinearColor Value;
};

USTRUCT()
struct FSwitchParameter
{
	GENERATED_BODY()

	UPROPERTY()
	FString Name;
	
	UPROPERTY()
	bool Value;
};

USTRUCT()
struct FMaskParameter
{
	GENERATED_BODY()

	UPROPERTY()
	FString Name;
	
	UPROPERTY()
	FLinearColor Value;
};

USTRUCT()
struct FParameterCollection
{
	GENERATED_BODY()
	
	UPROPERTY()
	TArray<FTextureParameter> Textures;

	UPROPERTY()
	TArray<FScalarParameter> Scalars;
	
	UPROPERTY()
	TArray<FVectorParameter> Vectors;

	UPROPERTY()
	TArray<FSwitchParameter> Switches;
	
	UPROPERTY()
	TArray<FMaskParameter> ComponentMasks;
};

USTRUCT()
struct FExportMaterial : public FParameterCollection
{
	GENERATED_BODY()
	
	UPROPERTY()
	FString Name;
	
	UPROPERTY()
	FString Path;
	
	UPROPERTY()
	FString BaseMaterialPath;
	
	UPROPERTY()
	int Slot;
	
	UPROPERTY()
	FString PhysMaterialName;

	UPROPERTY()
	TEnumAsByte<EBlendMode> BaseBlendMode;
	
	UPROPERTY()
	TEnumAsByte<EBlendMode> OverrideBlendMode;

	UPROPERTY()
	TEnumAsByte<ETranslucencyLightingMode> TranslucencyLightingMode;
	
	UPROPERTY()
	TEnumAsByte<EMaterialShadingModel> ShadingModel;
};

USTRUCT()
struct FExportTextureData
{
	GENERATED_BODY()

	UPROPERTY()
	int Hash;
	
	UPROPERTY()
	FTextureParameter Diffuse;
	
	UPROPERTY()
	FTextureParameter Normal;
	
	UPROPERTY()
	FTextureParameter Specular;
};
