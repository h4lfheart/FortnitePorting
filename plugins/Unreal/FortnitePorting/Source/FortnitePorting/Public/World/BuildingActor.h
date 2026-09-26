#pragma once
#include "Classes/BuildingTextureData.h"
#include "Engine/StaticMeshActor.h"
#include "BuildingActor.generated.h"

USTRUCT(BlueprintType)
struct FTextureDataInstance
{
	GENERATED_BODY()

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	int LayerIndex;
	
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TObjectPtr<UBuildingTextureData> TextureData;
};

UCLASS()
class ABuildingActor : public AStaticMeshActor
{
	GENERATED_BODY()
public:
	
	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FTextureDataInstance> TextureData;

	virtual void OnConstruction(const FTransform& Transform) override;
};
