#pragma once

#include "CoreMinimal.h"
#include "Engine/DataAsset.h"
#include "BuildingTextureData.generated.h"

UCLASS(BlueprintType)
class UBuildingTextureData : public UDataAsset
{
	GENERATED_BODY()

public:

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TObjectPtr<UTexture> Diffuse;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TObjectPtr<UTexture> Specular;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TObjectPtr<UTexture> Normal;

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TObjectPtr<UMaterialInstance> OverrideMaterial;
};
