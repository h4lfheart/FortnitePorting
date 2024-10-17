#pragma once
#include "Processing/Models/Data/ExportMaterial.h"
#include "BuildingTextureData.generated.h"

USTRUCT(BlueprintType)
struct FTextureDataItem
{
	GENERATED_BODY()
	
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FString Name;
	
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	UTexture* Texture;

	FTextureDataItem(const FString& Name, UTexture* Texture) : Name(Name), Texture(Texture)
	{
		
	}

	FTextureDataItem() : Name(""), Texture(nullptr)
	{
		
	}
};

USTRUCT(BlueprintType)
struct FBuildingTextureData
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FTextureDataItem Diffuse;
	
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FTextureDataItem Normals;
	
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	FTextureDataItem Specular;
};