#pragma once

#include "Archive/UEFormatReader.h"
#include "Data/UEModelData.h"

namespace UEFormat::Deserialize
{
	void ReadSkeleton(FUEFormatReader& Ar, FSkeletonData& OutSkeleton);
}
