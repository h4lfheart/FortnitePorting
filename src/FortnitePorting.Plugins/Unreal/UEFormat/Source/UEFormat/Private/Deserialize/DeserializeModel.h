#pragma once

#include "Archive/UEFormatReader.h"
#include "Data/UEModelData.h"

namespace UEFormat::Deserialize
{
	void ReadModel(FUEFormatReader& Ar, TArray<FLODData>& OutLODs, FSkeletonData& OutSkeleton);
}
