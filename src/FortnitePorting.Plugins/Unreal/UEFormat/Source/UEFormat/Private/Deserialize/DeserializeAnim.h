#pragma once

#include "Archive/UEFormatReader.h"
#include "Data/UEAnimData.h"

namespace UEFormat::Deserialize
{
	void ReadAnim(FUEFormatReader& Ar, FAnimData& OutAnim);
}
