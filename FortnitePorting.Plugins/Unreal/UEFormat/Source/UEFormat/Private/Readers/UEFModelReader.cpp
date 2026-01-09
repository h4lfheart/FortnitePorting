// Copyright © 2025 Marcel K. All rights reserved.

#include "Readers/UEFModelReader.h"
#include <string>
#include <vector>

#include "zstd.h"
#include "Misc/Compression.h"

std::string ReadString(std::ifstream& Ar, int32 Size)
{
    std::string String;
    String.resize(Size);
    Ar.read(String.data(), Size);
    return String;
}

std::string ReadFString(std::ifstream& Ar)
{
    int32 Size = ReadData<int32>(Ar);
    std::string String;
    String.resize(Size);
    Ar.read(String.data(), Size);
    return String;
}

FQuat4f ReadBufferQuat(const char* DataArray, int& Offset)
{
    float X = ReadBufferData<float>(DataArray, Offset);
    float Y = ReadBufferData<float>(DataArray, Offset);
    float Z = ReadBufferData<float>(DataArray, Offset);
    float W = ReadBufferData<float>(DataArray, Offset);
    return FQuat4f(X, Y, Z, W).GetNormalized(); // Directly return the value
}

std::string ReadBufferString(const char* DataArray, int& Offset, int32 Size)
{
    std::string String;
    String.resize(Size);
    std::memcpy(String.data(), &DataArray[Offset], Size);
    Offset += Size;
    return String;
}

std::string ReadBufferFString(const char* DataArray, int& Offset)
{
    int32 Size = ReadBufferData<int32>(DataArray, Offset);
    std::string String;
    String.resize(Size);
    std::memcpy(String.data(), &DataArray[Offset], Size);
    Offset += Size;
    return String;
}

UEFModelReader::UEFModelReader(const FString Filename) : Ar(ToCStr(Filename), std::ios::binary) {}

UEFModelReader::~UEFModelReader() { //Destructor
    if (Ar.is_open()) {
        Ar.close();
    }
}

bool UEFModelReader::Read() {
    std::string Magic = ReadString(Ar, GMAGIC.length());
    if (Magic != GMAGIC) return false;

    Header.Identifier = ReadFString(Ar);
    Header.FileVersionBytes = ReadData<std::byte>(Ar);
    Header.ObjectName = ReadFString(Ar);
    Header.IsCompressed = ReadData<bool>(Ar);

    if (Header.IsCompressed) {
        Header.CompressionType = ReadFString(Ar);
        Header.UncompressedSize = ReadData<int32>(Ar);
        Header.CompressedSize = ReadData<int32>(Ar);

        std::vector<char> CompressedBuffer(Header.CompressedSize);
        Ar.read(CompressedBuffer.data(), Header.CompressedSize);
        if (Ar.fail()) {
            UE_LOG(LogTemp, Error, TEXT("Error reading compressed data."));
            return false;
        }

        std::vector<char> UncompressedBuffer(Header.UncompressedSize);

        if (Header.CompressionType == "ZSTD")
            ZSTD_decompress(UncompressedBuffer.data(), Header.UncompressedSize, CompressedBuffer.data(), Header.CompressedSize);

        else if (Header.CompressionType == "GZIP")
            FCompression::UncompressMemory(NAME_Gzip, UncompressedBuffer.data(), Header.UncompressedSize, CompressedBuffer.data(), Header.CompressedSize);

        ReadBuffer(UncompressedBuffer.data(), Header.UncompressedSize);
    }
    else {
        const auto CurrentPos = Ar.tellg();
        Ar.seekg(0, std::ios::end);
        const auto RemainingSize = Ar.tellg() - CurrentPos;
        Ar.seekg(CurrentPos, std::ios::beg);
        
        std::vector<char> UncompressedBuffer(RemainingSize);
        Ar.read(UncompressedBuffer.data(), RemainingSize);
        if (Ar.fail()) {
            UE_LOG(LogTemp, Error, TEXT("Error reading uncompressed data."));
            return false;
        }

        ReadBuffer(UncompressedBuffer.data(), RemainingSize);
    }

    Ar.close();
    return true;
}

void UEFModelReader::ReadBuffer(const char* Buffer, int32 BufferSize) {
    int32 Offset = 0;

    while (Offset < BufferSize)
    {
        std::string ChunkName = ReadBufferFString(Buffer, Offset);
        int32 ArraySize = ReadBufferData<int32>(Buffer, Offset);
        int32 ByteSize = ReadBufferData<int32>(Buffer, Offset);

        if (ChunkName == "LODS")
        {
            LODs.SetNum(ArraySize);
            for (int32 index = 0; index < ArraySize; ++index) {
                std::string LODName = ReadBufferFString(Buffer, Offset);
                int32 LODByteSize = ReadBufferData<int32>(Buffer, Offset);
                ReadChunks(Buffer, Offset, LODByteSize, index);
            }
        }
        else if (ChunkName == "SKELETON")
            ReadChunks(Buffer, Offset, ByteSize, 0);
        else
            Offset += ByteSize;
    }
}

void UEFModelReader::ReadChunks(const char* Buffer, int32& Offset, int32 ByteSize, int32 LODIndex) {
    int32 InnerOffset = Offset; // Offset for nested data
    while (InnerOffset < Offset + ByteSize)
    {
        std::string InnerChunkName = ReadBufferFString(Buffer, InnerOffset);
        int32 InnerArraySize = ReadBufferData<int32>(Buffer, InnerOffset);
        int32 InnerByteSize = ReadBufferData<int32>(Buffer, InnerOffset);

        if (InnerChunkName == "VERTICES")
            ReadBufferArray(Buffer, InnerOffset, InnerArraySize, LODs[LODIndex].Vertices);
        else if (InnerChunkName == "INDICES")
            ReadBufferArray(Buffer, InnerOffset, InnerArraySize, LODs[LODIndex].Indices);
        else if (InnerChunkName == "NORMALS")
            ReadBufferArray(Buffer, InnerOffset, InnerArraySize, LODs[LODIndex].Normals);
        else if (InnerChunkName == "TANGENTS")
            ReadBufferArray(Buffer, InnerOffset, InnerArraySize, LODs[LODIndex].Tangents);
        else if (InnerChunkName == "VERTEXCOLORS")
        {
            LODs[LODIndex].VertexColors.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++)
            {
                LODs[LODIndex].VertexColors[i].Name = ReadBufferFString(Buffer, InnerOffset);
                LODs[LODIndex].VertexColors[i].Count = ReadBufferData<int32>(Buffer, InnerOffset);
                LODs[LODIndex].VertexColors[i].Data.SetNum(LODs[LODIndex].VertexColors[i].Count);
                ReadBufferArray(Buffer, InnerOffset, LODs[LODIndex].VertexColors[i].Count, LODs[LODIndex].VertexColors[i].Data);
            }
        }
        else if (InnerChunkName == "MATERIALS")
        {
            LODs[LODIndex].Materials.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++)
            {
                LODs[LODIndex].Materials[i].Name = ReadBufferFString(Buffer, InnerOffset);
                LODs[LODIndex].Materials[i].Path = ReadBufferFString(Buffer, InnerOffset);
                LODs[LODIndex].Materials[i].FirstIndex = ReadBufferData<int32>(Buffer, InnerOffset);
                LODs[LODIndex].Materials[i].NumFaces = ReadBufferData<int32>(Buffer, InnerOffset);
            }
        }
        else if (InnerChunkName == "TEXCOORDS")
        {
            LODs[LODIndex].TextureCoordinates.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++)
                {
                int32 UVCount = ReadBufferData<int32>(Buffer, InnerOffset);
                LODs[LODIndex].TextureCoordinates[i].SetNum(UVCount);
                for (auto j = 0; j < UVCount; j++)
                {
                    float U = ReadBufferData<float>(Buffer, InnerOffset);
                    float V = ReadBufferData<float>(Buffer, InnerOffset);
                    LODs[LODIndex].TextureCoordinates[i][j] = FVector2f(U,V);
                }
            }
        }
        else if (InnerChunkName == "SOCKETS")
        {
            Skeleton.Sockets.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++)
            {
                Skeleton.Sockets[i].SocketName = ReadBufferFString(Buffer, InnerOffset);
                Skeleton.Sockets[i].SocketParentName = ReadBufferFString(Buffer, InnerOffset);
                Skeleton.Sockets[i].SocketPos = ReadBufferData<FVector3f>(Buffer, InnerOffset);
                Skeleton.Sockets[i].SocketRot = ReadBufferQuat(Buffer, InnerOffset);
                Skeleton.Sockets[i].SocketScale = ReadBufferData<FVector3f>(Buffer, InnerOffset);
            }
        }
        else if (InnerChunkName == "BONES")
        {
            Skeleton.Bones.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++)
            {
                Skeleton.Bones[i].BoneName = ReadBufferFString(Buffer, InnerOffset);
                Skeleton.Bones[i].BoneParentIndex = ReadBufferData<int32>(Buffer, InnerOffset);
                Skeleton.Bones[i].BonePos = ReadBufferData<FVector3f>(Buffer, InnerOffset);
                Skeleton.Bones[i].BoneRot = ReadBufferQuat(Buffer, InnerOffset);
            }
        }
        else if (InnerChunkName == "WEIGHTS")
        {
            LODs[LODIndex].Weights.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++)
            {
                LODs[LODIndex].Weights[i].WeightBoneIndex = ReadBufferData<short>(Buffer, InnerOffset);
                LODs[LODIndex].Weights[i].WeightVertexIndex = ReadBufferData<int32>(Buffer, InnerOffset);
                LODs[LODIndex].Weights[i].WeightAmount = ReadBufferData<float>(Buffer, InnerOffset);
            }
        }
        else if (InnerChunkName == "MORPHTARGETS")
        {
            LODs[LODIndex].Morphs.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++)
            {
                LODs[LODIndex].Morphs[i].MorphName = ReadBufferFString(Buffer, InnerOffset);
                const auto DeltaNum = ReadBufferData<int32>(Buffer, InnerOffset);
                LODs[LODIndex].Morphs[i].MorphDeltas.SetNum(DeltaNum);
                for (auto j = 0; j < DeltaNum; j++)
                {
                    LODs[LODIndex].Morphs[i].MorphDeltas[j].MorphPosition = ReadBufferData<FVector3f>(Buffer, InnerOffset);
                    LODs[LODIndex].Morphs[i].MorphDeltas[j].MorphNormals = ReadBufferData<FVector3f>(Buffer, InnerOffset);
                    LODs[LODIndex].Morphs[i].MorphDeltas[j].MorphVertexIndex = ReadBufferData<int32>(Buffer, InnerOffset);
                }
            }
        }
        else if (InnerChunkName == "VIRTUALBONES")
        {
            Skeleton.VirtualBones.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++)
            {
                Skeleton.VirtualBones[i].SourceBoneName = ReadBufferFString(Buffer, InnerOffset);
                Skeleton.VirtualBones[i].TargetBoneName = ReadBufferFString(Buffer, InnerOffset);
                Skeleton.VirtualBones[i].VirtualBoneName = ReadBufferFString(Buffer, InnerOffset);
            }
        }
        else if (InnerChunkName == "METADATA")
        {
            Skeleton.Path = ReadBufferFString(Buffer, InnerOffset);
        }
        else
            InnerOffset += InnerByteSize;
    }
    Offset += ByteSize;
}