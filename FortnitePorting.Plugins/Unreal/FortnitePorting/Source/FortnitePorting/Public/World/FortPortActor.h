#pragma once
#include "BuildingTextureData.h"
#include "Engine/StaticMeshActor.h"
#include "FortPortActor.generated.h"

UCLASS()
class AFortPortActor : public AStaticMeshActor
{
	GENERATED_BODY()
public:

	UPROPERTY(EditAnywhere, BlueprintReadWrite)
	TArray<FBuildingTextureData> TextureDatas;

	virtual void OnConstruction(const FTransform& Transform) override;

	void AddTextureData(const FBuildingTextureData& TextureData)
	{
		TextureDatas.Add(TextureData);
		Modify();
	}
};
