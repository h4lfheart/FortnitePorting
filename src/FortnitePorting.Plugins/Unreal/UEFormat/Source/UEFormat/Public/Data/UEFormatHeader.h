#pragma once

#include "CoreMinimal.h"
#include "Version/EUEFormatVersion.h"
#include <string>

struct FUEFormatHeader
{
	std::string Identifier;
	EUEFormatVersion FileVersion = EUEFormatVersion::BeforeCustomVersionWasAdded;
	std::string ObjectName;
	std::string ObjectPath;
	bool bIsCompressed = false;
	std::string CompressionType;
	int32 CompressedSize = 0;
	int32 UncompressedSize = 0;
};
