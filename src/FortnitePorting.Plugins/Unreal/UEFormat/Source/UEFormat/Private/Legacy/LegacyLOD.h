#pragma once

#include "Archive/UEFormatReader.h"
#include "Data/UEModelData.h"

namespace UEFormat::Legacy
{
	void ReadLOD(FUEFormatReader& Ar, FLODData& OutLOD);
}
