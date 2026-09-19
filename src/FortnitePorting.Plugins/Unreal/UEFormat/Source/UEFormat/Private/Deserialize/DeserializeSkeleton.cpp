#include "Deserialize/DeserializeSkeleton.h"
#include "Deserialize/Attributes.h"

namespace UEFormat::Deserialize
{
	void ReadSkeleton(FUEFormatReader& Ar, FSkeletonData& OutSkeleton)
	{
		ForEachAttribute(Ar, [&](const std::string& Name, FUEFormatReader& Payload)
		{
			if (Name == "METADATA")
			{
				OutSkeleton.Path = Payload.ReadFString();
			}
			else if (Name == "BONES")
			{
				const int32 Count = Payload.ReadInt();
				OutSkeleton.Bones.SetNum(Count);
				for (int32 Index = 0; Index < Count; ++Index)
				{
					OutSkeleton.Bones[Index].BoneName = Payload.ReadFString();
					OutSkeleton.Bones[Index].BoneParentIndex = Payload.ReadInt();
					OutSkeleton.Bones[Index].BonePos = Payload.ReadVector();
					OutSkeleton.Bones[Index].BoneRot = Payload.ReadQuat();
					OutSkeleton.Bones[Index].BoneScale = Payload.ReadVector();
				}
			}
			else if (Name == "SOCKETS")
			{
				const int32 Count = Payload.ReadInt();
				OutSkeleton.Sockets.SetNum(Count);
				for (int32 Index = 0; Index < Count; ++Index)
				{
					OutSkeleton.Sockets[Index].SocketName = Payload.ReadFString();
					OutSkeleton.Sockets[Index].SocketParentName = Payload.ReadFString();
					OutSkeleton.Sockets[Index].SocketPos = Payload.ReadVector();
					OutSkeleton.Sockets[Index].SocketRot = Payload.ReadQuat();
					OutSkeleton.Sockets[Index].SocketScale = Payload.ReadVector();
				}
			}
			else if (Name == "VIRTUALBONES")
			{
				const int32 Count = Payload.ReadInt();
				OutSkeleton.VirtualBones.SetNum(Count);
				for (int32 Index = 0; Index < Count; ++Index)
				{
					OutSkeleton.VirtualBones[Index].SourceBoneName = Payload.ReadFString();
					OutSkeleton.VirtualBones[Index].TargetBoneName = Payload.ReadFString();
					OutSkeleton.VirtualBones[Index].VirtualBoneName = Payload.ReadFString();
				}
			}
			else
			{
				UE_LOG(LogTemp, Warning, TEXT("Unknown skeleton attribute: %s"), UTF8_TO_TCHAR(Name.c_str()));
			}
		});
	}
}
