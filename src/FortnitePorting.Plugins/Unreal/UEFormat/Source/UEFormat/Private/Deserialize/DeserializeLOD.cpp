#include "Deserialize/DeserializeLOD.h"
#include "Deserialize/Attributes.h"

namespace UEFormat::Deserialize
{
	void ReadLOD(FUEFormatReader& Ar, FLODData& OutLOD)
	{
		Ar.ReadFString();

		ForEachAttribute(Ar, [&](const std::string& Name, FUEFormatReader& Payload)
		{
			if (Name == "VERTICES")
			{
				Payload.ReadArray(Payload.ReadInt(), OutLOD.Vertices);
			}
			else if (Name == "NORMALS")
			{
				Payload.ReadArray(Payload.ReadInt(), OutLOD.Normals);
			}
			else if (Name == "TANGENTS")
			{
				Payload.ReadArray(Payload.ReadInt(), OutLOD.Tangents);
			}
			else if (Name == "INDICES")
			{
				Payload.ReadArray(Payload.ReadInt(), OutLOD.Indices);
			}
			else if (Name == "TEXCOORDS")
			{
				const int32 ChannelCount = Payload.ReadInt();
				OutLOD.TextureCoordinates.SetNum(ChannelCount);
				for (int32 ChannelIndex = 0; ChannelIndex < ChannelCount; ++ChannelIndex)
				{
					Payload.ReadFString();
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
			else if (Name == "VERTEXCOLORS")
			{
				const int32 Count = Payload.ReadInt();
				OutLOD.VertexColors.SetNum(Count);
				for (int32 Index = 0; Index < Count; ++Index)
				{
					OutLOD.VertexColors[Index].Name = Payload.ReadFString();
					OutLOD.VertexColors[Index].Count = Payload.ReadInt();
					Payload.ReadColorArray(OutLOD.VertexColors[Index].Count, OutLOD.VertexColors[Index].Data);
				}
			}
			else if (Name == "MATERIALS")
			{
				const int32 Count = Payload.ReadInt();
				OutLOD.Materials.SetNum(Count);
				for (int32 Index = 0; Index < Count; ++Index)
				{
					OutLOD.Materials[Index].Name = Payload.ReadFString();
					OutLOD.Materials[Index].Path = Payload.ReadFString();
					OutLOD.Materials[Index].FirstIndex = Payload.ReadInt();
					OutLOD.Materials[Index].NumFaces = Payload.ReadInt();
				}
			}
			else if (Name == "WEIGHTS")
			{
				const int32 Count = Payload.ReadInt();
				OutLOD.Weights.SetNum(Count);
				for (int32 Index = 0; Index < Count; ++Index)
				{
					OutLOD.Weights[Index].WeightBoneIndex = static_cast<short>(Payload.ReadUShort());
					OutLOD.Weights[Index].WeightVertexIndex = Payload.ReadInt();
					OutLOD.Weights[Index].WeightAmount = Payload.ReadFloat();
				}
			}
			else if (Name == "MORPHTARGETS")
			{
				const int32 Count = Payload.ReadInt();
				OutLOD.Morphs.SetNum(Count);
				for (int32 MorphIndex = 0; MorphIndex < Count; ++MorphIndex)
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
			else
			{
				UE_LOG(LogTemp, Warning, TEXT("Unknown LOD attribute: %s"), UTF8_TO_TCHAR(Name.c_str()));
			}
		});
	}
}
