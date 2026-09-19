#pragma once

#include "Archive/UEFormatReader.h"
#include "Data/UEModelData.h"

namespace UEFormat::Deserialize
{
	void ReadLOD(FUEFormatReader& Ar, FLODData& OutLOD);
}
