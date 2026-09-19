#include "Deserialize/DeserializeModel.h"
#include "Deserialize/Attributes.h"
#include "Deserialize/DeserializeLOD.h"
#include "Deserialize/DeserializeSkeleton.h"

namespace UEFormat::Deserialize
{
	void ReadModel(FUEFormatReader& Ar, TArray<FLODData>& OutLODs, FSkeletonData& OutSkeleton)
	{
		ForEachAttribute(Ar, [&](const std::string& Name, FUEFormatReader& Payload)
		{
			if (Name == "LODS")
			{
				const int32 Count = Payload.ReadInt();
				OutLODs.SetNum(Count);
				for (int32 Index = 0; Index < Count; ++Index)
				{
					ReadLOD(Payload, OutLODs[Index]);
				}
			}
			else if (Name == "SKELETON")
			{
				ReadSkeleton(Payload, OutSkeleton);
			}
			else if (Name == "COLLISION")
			{
				// unsupported
			}
			else
			{
				UE_LOG(LogTemp, Warning, TEXT("Unknown model attribute: %s"), UTF8_TO_TCHAR(Name.c_str()));
			}
		});
	}
}
