#pragma once

#include "Archive/UEFormatReader.h"
#include "Data/UEAnimData.h"

namespace UEFormat::Legacy
{
	void ReadAnim(FUEFormatReader& Ar, FAnimData& OutAnim);
}
