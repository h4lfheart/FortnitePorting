#pragma once

#include "CoreMinimal.h"
#include "InterchangeGenericTexturePipeline.h"
#include "Engine/Texture.h"
#include "FortnitePortingTexturePipeline.generated.h"

/**
 * Thin pipeline subclass that bakes per-texture sRGB and CompressionSettings
 * from JSON into the Interchange factory node before the texture is built,
 * eliminating the PostEditChange mutation race of the legacy UTextureFactory path.
 */
UCLASS(Transient)
class FORTNITEPORTING_API UFortnitePortingTexturePipeline : public UInterchangeGenericTexturePipeline
{
	GENERATED_BODY()

public:
	bool bWantSRGB = true;
	TextureCompressionSettings WantCompression = TC_Default;

	virtual void ExecutePipeline(UInterchangeBaseNodeContainer* InBaseNodeContainer,
		const TArray<UInterchangeSourceData*>& InSourceDatas,
		const FString& ContentBasePath) override;
};
