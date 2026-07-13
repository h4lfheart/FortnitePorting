#include "Processing/FortnitePortingTexturePipeline.h"

#include "Nodes/InterchangeBaseNodeContainer.h"
#include "InterchangeTextureFactoryNode.h"

void UFortnitePortingTexturePipeline::ExecutePipeline(
	UInterchangeBaseNodeContainer* InBaseNodeContainer,
	const TArray<UInterchangeSourceData*>& InSourceDatas,
	const FString& ContentBasePath)
{
	// Let the parent pipeline build all factory nodes first.
	Super::ExecutePipeline(InBaseNodeContainer, InSourceDatas, ContentBasePath);

	// Then stamp our per-texture sRGB and CompressionSettings onto every texture
	// factory node the parent created, so the asset is built once, correctly.
	InBaseNodeContainer->IterateNodesOfType<UInterchangeTextureFactoryNode>(
		[this](const FString& /*NodeUid*/, UInterchangeTextureFactoryNode* FactoryNode)
		{
			FactoryNode->SetCustomSRGB(bWantSRGB);
			FactoryNode->SetCustomCompressionSettings(static_cast<uint8>(WantCompression));
		});
}
