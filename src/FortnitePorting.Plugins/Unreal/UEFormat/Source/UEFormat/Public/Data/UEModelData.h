#pragma once

#include "CoreMinimal.h"
#include "Math/Color.h"
#include "Math/Quat.h"
#include <string>

struct FVertexColorChunk
{
	std::string Name;
	int32 Count = 0;
	TArray<FColor> Data;
};

struct FWeightChunk
{
	short WeightBoneIndex = 0;
	int32 WeightVertexIndex = 0;
	float WeightAmount = 0.f;
};

struct FBoneChunk
{
	std::string BoneName;
	int32 BoneParentIndex = 0;
	FVector3f BonePos = FVector3f::ZeroVector;
	FQuat4f BoneRot = FQuat4f::Identity;
	FVector3f BoneScale = FVector3f(1.f, 1.f, 1.f);
};

struct FSocketChunk
{
	std::string SocketName;
	std::string SocketParentName;
	FVector3f SocketPos = FVector3f::ZeroVector;
	FQuat4f SocketRot = FQuat4f::Identity;
	FVector3f SocketScale = FVector3f(1.f, 1.f, 1.f);
};

struct FMaterialChunk
{
	std::string Name;
	std::string Path;
	int32 FirstIndex = 0;
	int32 NumFaces = 0;
};

struct FMorphTargetDataChunk
{
	FVector3f MorphPosition = FVector3f::ZeroVector;
	FVector3f MorphNormals = FVector3f::ZeroVector;
	int32 MorphVertexIndex = 0;
};

struct FMorphTargetChunk
{
	std::string MorphName;
	TArray<FMorphTargetDataChunk> MorphDeltas;
};

struct FVirtualBoneChunk
{
	std::string SourceBoneName;
	std::string TargetBoneName;
	std::string VirtualBoneName;
};

struct FLODData
{
	TArray<FVector3f> Vertices;
	TArray<int32> Indices;
	TArray<FVector4f> Normals;
	TArray<FVector3f> Tangents;
	TArray<FVertexColorChunk> VertexColors;
	TArray<TArray<FVector2f>> TextureCoordinates;
	TArray<FMaterialChunk> Materials;
	TArray<FWeightChunk> Weights;
	TArray<FMorphTargetChunk> Morphs;
};

struct FSkeletonData
{
	std::string Path;
	TArray<FBoneChunk> Bones;
	TArray<FSocketChunk> Sockets;
	TArray<FVirtualBoneChunk> VirtualBones;
};
