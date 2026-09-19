#include "Readers/UEFAnimReader.h"
#include "Archive/UEFormatReader.h"
#include "Deserialize/DeserializeAnim.h"
#include "Legacy/LegacyAnim.h"
#include "Version/EUEFormatVersion.h"

UEFAnimReader::UEFAnimReader(const FString InFilename)
	: Filename(InFilename)
{
}

bool UEFAnimReader::Read()
{
	FUEFormatReader Body;
	if (!UEFormat::LoadFile(Filename, Header, Body))
	{
		return false;
	}

	FAnimData Anim;
	if (Header.FileVersion >= EUEFormatVersion::AttributeFormatRestructure)
	{
		UEFormat::Deserialize::ReadAnim(Body, Anim);
	}
	else
	{
		UEFormat::Legacy::ReadAnim(Body, Anim);
	}

	NumFrames = Anim.NumFrames;
	FramesPerSecond = Anim.FramesPerSecond;
	RefPosePath = MoveTemp(Anim.RefPosePath);
	AdditiveAnimType = Anim.AdditiveAnimType;
	RefPoseType = Anim.RefPoseType;
	RefFrameIndex = Anim.RefFrameIndex;
	Tracks = MoveTemp(Anim.Tracks);
	Curves = MoveTemp(Anim.Curves);
	return true;
}
