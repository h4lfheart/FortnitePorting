#include "Readers/UEFModelReader.h"

#include <string>

#include "zstd.h"
#include "Misc/Compression.h"

FQuat4f ReadQuat(std::ifstream& Ar)
{
    float X = ReadData<float>(Ar);
    float Y = ReadData<float>(Ar);
    float Z = ReadData<float>(Ar);
    float W = ReadData<float>(Ar);
    auto Data = FQuat4f(X, Y, Z, W);
    return Data;
}

std::string ReadString(std::ifstream& Ar, int32 Size)
{
    std::string String;
    String.resize(Size);
    Ar.read(&String[0], Size);  //Read data directly into string buffer
    return String;
}

std::string ReadFString(std::ifstream& Ar)
{
    int32 Size = ReadData<int32>(Ar);
    std::string String;
    String.resize(Size);
    Ar.read(&String[0], Size);
    return String;
}

FQuat4f ReadBufferQuat(const char* DataArray, int& Offset)
{
    float X = ReadBufferData<float>(DataArray, Offset);
    float Y = ReadBufferData<float>(DataArray, Offset);
    float Z = ReadBufferData<float>(DataArray, Offset);
    float W = ReadBufferData<float>(DataArray, Offset);
    auto Data = FQuat4f(X, Y, Z, W);
    return Data;
}

std::string ReadBufferString(const char* DataArray, int& Offset, int32 Size)
{
    std::string String;
    String.resize(Size);
    std::memcpy(&String[0], &DataArray[Offset], Size);
    Offset += Size;
    return String;
}

std::string ReadBufferFString(const char* DataArray, int& Offset)
{
    int32 Size = ReadBufferData<int32>(DataArray, Offset);
    std::string String;
    String.resize(Size);
    std::memcpy(&String[0], &DataArray[Offset], Size);
    Offset += Size;
    return String;
}

UEFModelReader::UEFModelReader(const FString Filename) {
    Ar.open(ToCStr(Filename), std::ios::binary);
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
        
        char* CompressedBuffer = new char[Header.CompressedSize];
        Ar.read(CompressedBuffer, Header.CompressedSize);
        
        char* UncompressedBuffer = new char[Header.UncompressedSize];
        
        if (Header.CompressionType == "ZSTD")
        {
            ZSTD_decompress(UncompressedBuffer, Header.UncompressedSize, CompressedBuffer, Header.CompressedSize);
        }
        else if (Header.CompressionType == "GZIP")
        {
            FCompression::UncompressMemory(NAME_Gzip, UncompressedBuffer, Header.UncompressedSize, CompressedBuffer, Header.CompressedSize);
        }
        
        ReadBuffer(UncompressedBuffer, Header.UncompressedSize);
    }
    else
    {
        //find out remaining size
        const auto CurrentPos = Ar.tellg();
        Ar.seekg(0, std::ios::end);
        const auto EndPos = Ar.tellg();
        Ar.seekg(CurrentPos, std::ios::beg);

        const auto RemainingSize = EndPos - CurrentPos;
        char* UncompressedBuffer = new char[RemainingSize];

        Ar.read(UncompressedBuffer, RemainingSize);
        ReadBuffer(UncompressedBuffer, RemainingSize);
        delete[] UncompressedBuffer;
    }
    Ar.close();
    return true;
}

void UEFModelReader::ReadBuffer(const char* Buffer, int32 BufferSize) {
    int32 Offset = 0;

    while (Offset < BufferSize) {
        std::string ChunkName = ReadBufferFString(Buffer, Offset);
        int32 ArraySize = ReadBufferData<int32>(Buffer, Offset);
        int32 ByteSize = ReadBufferData<int32>(Buffer, Offset);

        if (ChunkName == "LODS") {
            LODs.SetNum(ArraySize);
            for (int32 index = 0; index < ArraySize; ++index) {
                std::string LODName = ReadBufferFString(Buffer, Offset);
                int32 LODByteSize = ReadBufferData<int32>(Buffer, Offset);
                ReadChunks(Buffer, Offset, LODName, 0, LODByteSize, index);
            }
        } else if (ChunkName == "SKELETON") {
            ReadChunks(Buffer, Offset, ChunkName, 0, ByteSize, 0);
        } else {
            Offset += ByteSize;
        }
    }
}

void UEFModelReader::ReadChunks(const char* Buffer, int32& Offset, const std::string& ChunkName, int32 ArraySize, int32 ByteSize, int32 LODIndex) {
    int32 InnerOffset = Offset; // Offset for nested data
    while (InnerOffset < Offset + ByteSize) {
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
        else if (InnerChunkName == "VERTEXCOLORS") {
            LODs[LODIndex].VertexColors.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++) {
                LODs[LODIndex].VertexColors[i].Name = ReadBufferFString(Buffer, InnerOffset);
                LODs[LODIndex].VertexColors[i].Count = ReadBufferData<int32>(Buffer, InnerOffset);
                LODs[LODIndex].VertexColors[i].Data.SetNum(LODs[LODIndex].VertexColors[i].Count);
                ReadBufferArray(Buffer, InnerOffset, LODs[LODIndex].VertexColors[i].Count, LODs[LODIndex].VertexColors[i].Data);
            }
        } else if (InnerChunkName == "MATERIALS") {
            LODs[LODIndex].Materials.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++) {
                LODs[LODIndex].Materials[i].MatIndex = i;
                LODs[LODIndex].Materials[i].Name = ReadBufferFString(Buffer, InnerOffset);
                LODs[LODIndex].Materials[i].FirstIndex = ReadBufferData<int32>(Buffer, InnerOffset);
                LODs[LODIndex].Materials[i].NumFaces = ReadBufferData<int32>(Buffer, InnerOffset);
            }
        } else if (InnerChunkName == "TEXCOORDS") {
            LODs[LODIndex].TextureCoordinates.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++) {
                int32 UVCount = ReadBufferData<int32>(Buffer, InnerOffset);
                LODs[LODIndex].TextureCoordinates[i].SetNum(UVCount);
                for (auto j = 0; j < UVCount; j++) {
                    float U = ReadBufferData<float>(Buffer, InnerOffset);
                    float V = ReadBufferData<float>(Buffer, InnerOffset);
                    LODs[LODIndex].TextureCoordinates[i][j] = FVector2f(U, 1 - V);
                }
            }
        } else if (InnerChunkName == "SOCKETS") {
            Skeleton.Sockets.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++) {
                Skeleton.Sockets[i].SocketName = ReadBufferFString(Buffer, InnerOffset);
                Skeleton.Sockets[i].SocketParentName = ReadBufferFString(Buffer, InnerOffset);
                Skeleton.Sockets[i].SocketPos = ReadBufferData<FVector3f>(Buffer, InnerOffset);
                Skeleton.Sockets[i].SocketRot = ReadBufferQuat(Buffer, InnerOffset);
                Skeleton.Sockets[i].SocketScale = ReadBufferData<FVector3f>(Buffer, InnerOffset);
            }
        } else if (InnerChunkName == "BONES") {
            Skeleton.Bones.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++) {
                Skeleton.Bones[i].BoneName = ReadBufferFString(Buffer, InnerOffset);
                Skeleton.Bones[i].BoneParentIndex = ReadBufferData<int32>(Buffer, InnerOffset);
                Skeleton.Bones[i].BonePos = ReadBufferData<FVector3f>(Buffer, InnerOffset);
                Skeleton.Bones[i].BoneRot = ReadBufferQuat(Buffer, InnerOffset);
            }
        } else if (InnerChunkName == "WEIGHTS") {
            LODs[LODIndex].Weights.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++) {
                LODs[LODIndex].Weights[i].WeightBoneIndex = ReadBufferData<short>(Buffer, InnerOffset);
                LODs[LODIndex].Weights[i].WeightVertexIndex = ReadBufferData<int32>(Buffer, InnerOffset);
                LODs[LODIndex].Weights[i].WeightAmount = ReadBufferData<float>(Buffer, InnerOffset);
            }
        } else if (InnerChunkName == "MORPHTARGETS") {
            LODs[LODIndex].Morphs.SetNum(InnerArraySize);
            for (auto i = 0; i < InnerArraySize; i++) {
                LODs[LODIndex].Morphs[i].MorphName = ReadBufferFString(Buffer, InnerOffset);
                const auto DeltaNum = ReadBufferData<int32>(Buffer, InnerOffset);
                LODs[LODIndex].Morphs[i].MorphDeltas.SetNum(DeltaNum);
                for (auto j = 0; j < DeltaNum; j++) {
                    LODs[LODIndex].Morphs[i].MorphDeltas[j].MorphPosition = ReadBufferData<FVector3f>(Buffer, InnerOffset);
                    LODs[LODIndex].Morphs[i].MorphDeltas[j].MorphNormals = ReadBufferData<FVector3f>(Buffer, InnerOffset);
                    LODs[LODIndex].Morphs[i].MorphDeltas[j].MorphVertexIndex = ReadBufferData<int32>(Buffer, InnerOffset);
                }
            }
        } else {
            InnerOffset += InnerByteSize;
        }
    }
    Offset += ByteSize;
}
