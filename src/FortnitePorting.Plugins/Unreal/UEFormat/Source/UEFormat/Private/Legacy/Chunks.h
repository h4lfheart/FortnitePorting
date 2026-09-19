#pragma once

#include "Archive/UEFormatReader.h"
#include <string>

namespace UEFormat::Legacy
{
	template<typename TCallback>
	void ForEachChunk(FUEFormatReader& Ar, TCallback&& Callback)
	{
		while (!Ar.AtEnd())
		{
			const std::string Name = Ar.ReadFString();
			const int32 ArraySize = Ar.ReadInt();
			const int32 ByteSize = Ar.ReadInt();
			const int32 Start = Ar.Tell();
			Callback(Name, ArraySize, Ar);
			Ar.Seek(Start + ByteSize);
		}
	}
}
