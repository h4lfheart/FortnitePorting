#pragma once

#include "Archive/UEFormatReader.h"
#include <string>

namespace UEFormat::Deserialize
{
	template<typename TCallback>
	void ForEachAttribute(FUEFormatReader& Ar, TCallback&& Callback)
	{
		const int32 Count = Ar.ReadInt();
		for (int32 Index = 0; Index < Count; ++Index)
		{
			const std::string Name = Ar.ReadFString();
			const int32 ByteSize = Ar.ReadInt();
			FUEFormatReader Payload = Ar.Chunk(ByteSize);
			Callback(Name, Payload);
		}
	}
}
