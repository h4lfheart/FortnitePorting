#include "Legacy/LegacySkeleton.h"
#include "Legacy/Chunks.h"

namespace UEFormat::Legacy
{
	void ReadSkeleton(FUEFormatReader& Ar, FSkeletonData& OutSkeleton)
	{
		ForEachChunk(Ar, [&](const std::string& Name, int32 ArraySize, FUEFormatReader& Payload)
		{
			if (Name == "METADATA")
			{
				OutSkeleton.Path = Payload.ReadFString();
			}
			else if (Name == "BONES")
			{
				OutSkeleton.Bones.SetNum(ArraySize);
				for (int32 Index = 0; Index < ArraySize; ++Index)
				{
					OutSkeleton.Bones[Index].BoneName = Payload.ReadFString();
					OutSkeleton.Bones[Index].BoneParentIndex = Payload.ReadInt();
					OutSkeleton.Bones[Index].BonePos = Payload.ReadVector();
					OutSkeleton.Bones[Index].BoneRot = Payload.ReadQuat();
					OutSkeleton.Bones[Index].BoneScale = FVector3f(1.f, 1.f, 1.f);
				}
			}
			else if (Name == "SOCKETS")
			{
				OutSkeleton.Sockets.SetNum(ArraySize);
				for (int32 Index = 0; Index < ArraySize; ++Index)
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
				OutSkeleton.VirtualBones.SetNum(ArraySize);
				for (int32 Index = 0; Index < ArraySize; ++Index)
				{
					OutSkeleton.VirtualBones[Index].SourceBoneName = Payload.ReadFString();
					OutSkeleton.VirtualBones[Index].TargetBoneName = Payload.ReadFString();
					OutSkeleton.VirtualBones[Index].VirtualBoneName = Payload.ReadFString();
				}
			}
		});
	}
}
