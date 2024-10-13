#pragma once
#include <fstream>
#include "Math/Quat.h"
#include "Containers/Array.h"

template<typename T>
void ReadArray(std::ifstream& Ar, int ArraySize, TArray<T>& Data) {
    Data.SetNum(ArraySize);
    for (auto i = 0; i < ArraySize; i++) {
        Ar.read(reinterpret_cast<char*>(&Data[i]), sizeof(T));
    }
}

template<typename T>
T ReadData(std::ifstream& Ar) {
    T Data;
    Ar.read(reinterpret_cast<char*>(&Data), sizeof(T));
    return Data;
}

FQuat4f ReadQuat(std::ifstream& Ar);

std::string ReadString(std::ifstream& Ar, int32 Size);

std::string ReadFString(std::ifstream& Ar);

template<typename T>
void ReadBufferArray(const char* DataArray, int& Offset, int ArraySize, TArray<T>& Data) {
    Data.SetNum(ArraySize);
    for (auto i = 0; i < ArraySize; i++) {
        std::memcpy(&Data[i], &DataArray[Offset], sizeof(T));
        Offset += sizeof(T);
    }
}

template<typename T>
T ReadBufferData(const char* DataArray, int& Offset) {
    T Data;
    std::memcpy(&Data, &DataArray[Offset], sizeof(T));
    Offset += sizeof(T);
    return Data;
}

FQuat4f ReadBufferQuat(const char* DataArray, int& Offset);

std::string ReadBufferString(const char* DataArray, int& Offset, int32 Size);

std::string ReadBufferFString(const char* DataArray, int& Offset);

struct FVertexColorChunk {
    std::string Name;
    int32 Count;
    TArray<FColor> Data;
};
struct FWeightChunk {
    short WeightBoneIndex;
    int32 WeightVertexIndex;
    float WeightAmount;
};
struct FBoneChunk {
    std::string BoneName;
    int32 BoneParentIndex;
    FVector3f BonePos;
    FQuat4f BoneRot;
};
struct FSocketChunk {
    std::string SocketName;
    std::string SocketParentName;
    FVector3f SocketPos;
    FQuat4f SocketRot;
    FVector3f SocketScale;
};
struct FMaterialChunk {
    int32 MatIndex;
    std::string Name;
    int32 FirstIndex;
    int32 NumFaces;
};
struct FMorphTargetDataChunk {
    FVector3f MorphPosition;
    FVector3f MorphNormals;
    int32 MorphVertexIndex;
};
struct FMorphTargetChunk {
    std::string MorphName;
    TArray<FMorphTargetDataChunk> MorphDeltas;
};
struct FUEFormatHeader {
    std::string Identifier;
    std::byte FileVersionBytes;
    std::string ObjectName;
    bool IsCompressed;
    std::string CompressionType;
    int32 CompressedSize;
    int32 UncompressedSize;
};

struct FLODData {
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

struct FSkeletonData {
    TArray<FBoneChunk> Bones;
    TArray<FSocketChunk> Sockets;
};

class UEFModelReader {
public:
    UEFModelReader(const FString Filename);
    bool Read();
    void ReadBuffer(const char* Buffer, int BufferSize);

    const std::string GMAGIC = "UEFORMAT";

    FUEFormatHeader Header;
    TArray<FLODData> LODs;
    FSkeletonData Skeleton;

private:
    std::ifstream Ar;
    void ReadLODChunk(const char* Buffer, int& Offset, int LODIndex);
    void ReadChunks(const char* Buffer, int& Offset, const std::string& ChunkName, int32 ArraySize, int32 ByteSize, int LODIndex);
};

