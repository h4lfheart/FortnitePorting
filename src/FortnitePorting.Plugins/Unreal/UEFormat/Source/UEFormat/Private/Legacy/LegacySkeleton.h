#pragma once

#include "Archive/UEFormatReader.h"
#include "Data/UEModelData.h"

namespace UEFormat::Legacy
{
	void ReadSkeleton(FUEFormatReader& Ar, FSkeletonData& OutSkeleton);
}
