#pragma once
#include <fstream>
#include "UEFModelReader.h"
#include "Containers/Array.h"
#include "Math/Quat.h"

struct FFloatKey
{
	int32 Frame;
	float FloatValue;
};
struct FVectorKey
{
	int32 Frame;
	FVector3f VectorValue;
};
struct FQuatKey
{
	int32 Frame;
	FQuat4f QuatValue;
};
struct FCurve
{
	std::string CurveName;
	TArray<FFloatKey> CurveKeys;
};
struct FTrack
{
	std::string TrackName;
	TArray<FVectorKey> TrackPosKeys;
	TArray<FQuatKey> TrackRotKeys;
	TArray<FVectorKey> TrackScaleKeys;
};

class UEFAnimReader
{
public:
	UEFAnimReader(const FString Filename);
	bool Read();
	void ReadArchive(std::ifstream& Archive);

	const std::string GMAGIC = "UEFORMAT";
	const std::string GZIP = "GZIP";
	const std::string ZSTD = "ZSTD";
	const std::string ANIM_IDENTIFIER = "UEANIM";

	FUEFormatHeader Header;
	int32 NumFrames;
	float FramesPerSecond;
	TArray<FTrack> Tracks;
	TArray<FCurve> Curves;

private:
	std::ifstream Ar;
};

