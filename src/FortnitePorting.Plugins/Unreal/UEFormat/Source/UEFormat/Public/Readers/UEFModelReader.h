#pragma once

#include "CoreMinimal.h"
#include "Data/UEFormatHeader.h"
#include "Data/UEModelData.h"

class UEFORMAT_API UEFModelReader
{
public:
	UEFModelReader(const FString Filename);

	bool Read();

	FUEFormatHeader Header;
	TArray<FLODData> LODs;
	FSkeletonData Skeleton;

private:
	FString Filename;
};
