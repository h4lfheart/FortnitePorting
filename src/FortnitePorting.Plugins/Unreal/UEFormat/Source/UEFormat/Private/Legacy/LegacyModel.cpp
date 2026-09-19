#include "Legacy/LegacyModel.h"
#include "Legacy/Chunks.h"
#include "Legacy/LegacyLOD.h"
#include "Legacy/LegacySkeleton.h"

namespace UEFormat::Legacy
{
	void ReadModel(FUEFormatReader& Ar, TArray<FLODData>& OutLODs, FSkeletonData& OutSkeleton)
	{
		ForEachChunk(Ar, [&](const std::string& Name, int32 ArraySize, FUEFormatReader& Payload)
		{
			if (Name == "LODS")
			{
				OutLODs.SetNum(ArraySize);
				for (int32 LODIndex = 0; LODIndex < ArraySize; ++LODIndex)
				{
					Payload.ReadFString();
					const int32 LODByteSize = Payload.ReadInt();
					FUEFormatReader LODAr = Payload.Chunk(LODByteSize);
					ReadLOD(LODAr, OutLODs[LODIndex]);
				}
			}
			else if (Name == "SKELETON")
			{
				ReadSkeleton(Payload, OutSkeleton);
			}
			else if (Name == "COLLISION")
			{
				// ignored/unsupported
			}
		});
	}
}
