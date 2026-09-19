#pragma once

#include "CoreMinimal.h"
#include "Data/UEAnimData.h"
#include "Data/UEFormatHeader.h"
#include <string>

class UEFORMAT_API UEFAnimReader
{
public:
	UEFAnimReader(const FString Filename);

	bool Read();

	FUEFormatHeader Header;

	int32 NumFrames = 0;
	float FramesPerSecond = 0.f;
	std::string RefPosePath;
	EAdditiveAnimationType AdditiveAnimType = AAT_None;
	EAdditiveBasePoseType RefPoseType = ABPT_None;
	int32 RefFrameIndex = 0;
	TArray<FTrack> Tracks;
	TArray<FCurve> Curves;

private:
	FString Filename;
};
