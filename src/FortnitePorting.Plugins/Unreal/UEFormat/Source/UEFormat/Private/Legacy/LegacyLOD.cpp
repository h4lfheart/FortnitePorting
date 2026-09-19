#include "Legacy/LegacyLOD.h"
#include "Legacy/Chunks.h"

namespace UEFormat::Legacy
{
	void ReadLOD(FUEFormatReader& Ar, FLODData& OutLOD)
	{
		ForEachChunk(Ar, [&](const std::string& Name, int32 ArraySize, FUEFormatReader& Payload)
		{
			if (Name == "VERTICES")
			{
				Payload.ReadArray(ArraySize, OutLOD.Vertices);
			}
			else if (Name == "INDICES")
			{
				Payload.ReadArray(ArraySize, OutLOD.Indices);
			}
			else if (Name == "NORMALS")
			{
				Payload.ReadArray(ArraySize, OutLOD.Normals);
			}
			else if (Name == "TANGENTS")
			{
				Payload.ReadArray(ArraySize, OutLOD.Tangents);
			}
			else if (Name == "VERTEXCOLORS")
			{
				OutLOD.VertexColors.SetNum(ArraySize);
				for (int32 Index = 0; Index < ArraySize; ++Index)
				{
					OutLOD.VertexColors[Index].Name = Payload.ReadFString();
					OutLOD.VertexColors[Index].Count = Payload.ReadInt();
					Payload.ReadColorArray(OutLOD.VertexColors[Index].Count, OutLOD.VertexColors[Index].Data);
				}
			}
			else if (Name == "MATERIALS")
			{
				OutLOD.Materials.SetNum(ArraySize);
				for (int32 Index = 0; Index < ArraySize; ++Index)
				{
					OutLOD.Materials[Index].Name = Payload.ReadFString();
					OutLOD.Materials[Index].Path = Payload.ReadFString();
					OutLOD.Materials[Index].FirstIndex = Payload.ReadInt();
					OutLOD.Materials[Index].NumFaces = Payload.ReadInt();
				}
			}
			else if (Name == "TEXCOORDS")
			{
				OutLOD.TextureCoordinates.SetNum(ArraySize);
				for (int32 ChannelIndex = 0; ChannelIndex < ArraySize; ++ChannelIndex)
				{
					const int32 UVCount = Payload.ReadInt();
					OutLOD.TextureCoordinates[ChannelIndex].SetNum(UVCount);
					for (int32 UVIndex = 0; UVIndex < UVCount; ++UVIndex)
					{
						const float U = Payload.ReadFloat();
						const float V = Payload.ReadFloat();
						OutLOD.TextureCoordinates[ChannelIndex][UVIndex] = FVector2f(U, V);
					}
				}
			}
			else if (Name == "WEIGHTS")
			{
				OutLOD.Weights.SetNum(ArraySize);
				for (int32 Index = 0; Index < ArraySize; ++Index)
				{
					OutLOD.Weights[Index].WeightBoneIndex = Payload.ReadShort();
					OutLOD.Weights[Index].WeightVertexIndex = Payload.ReadInt();
					OutLOD.Weights[Index].WeightAmount = Payload.ReadFloat();
				}
			}
			else if (Name == "MORPHTARGETS")
			{
				OutLOD.Morphs.SetNum(ArraySize);
				for (int32 MorphIndex = 0; MorphIndex < ArraySize; ++MorphIndex)
				{
					OutLOD.Morphs[MorphIndex].MorphName = Payload.ReadFString();
					const int32 DeltaNum = Payload.ReadInt();
					OutLOD.Morphs[MorphIndex].MorphDeltas.SetNum(DeltaNum);
					for (int32 DeltaIndex = 0; DeltaIndex < DeltaNum; ++DeltaIndex)
					{
						OutLOD.Morphs[MorphIndex].MorphDeltas[DeltaIndex].MorphPosition = Payload.ReadVector();
						OutLOD.Morphs[MorphIndex].MorphDeltas[DeltaIndex].MorphNormals = Payload.ReadVector();
						OutLOD.Morphs[MorphIndex].MorphDeltas[DeltaIndex].MorphVertexIndex = Payload.ReadInt();
					}
				}
			}
		});
	}
}
