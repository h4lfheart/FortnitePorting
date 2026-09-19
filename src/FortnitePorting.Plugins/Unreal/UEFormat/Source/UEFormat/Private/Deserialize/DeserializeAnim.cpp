#include "Deserialize/DeserializeAnim.h"
#include "Deserialize/Attributes.h"

namespace UEFormat::Deserialize
{
	static void ReadVectorKeys(FUEFormatReader& Ar, TArray<FVectorKey>& OutKeys)
	{
		const int32 Count = Ar.ReadInt();
		OutKeys.SetNum(Count);
		for (int32 Index = 0; Index < Count; ++Index)
		{
			OutKeys[Index].Frame = Ar.ReadInt();
			OutKeys[Index].VectorValue = Ar.ReadVector();
		}
	}

	static void ReadQuatKeys(FUEFormatReader& Ar, TArray<FQuatKey>& OutKeys)
	{
		const int32 Count = Ar.ReadInt();
		OutKeys.SetNum(Count);
		for (int32 Index = 0; Index < Count; ++Index)
		{
			OutKeys[Index].Frame = Ar.ReadInt();
			OutKeys[Index].QuatValue = Ar.ReadQuat();
		}
	}

	void ReadAnim(FUEFormatReader& Ar, FAnimData& OutAnim)
	{
		ForEachAttribute(Ar, [&](const std::string& Name, FUEFormatReader& Payload)
		{
			if (Name == "METADATA")
			{
				OutAnim.NumFrames = Payload.ReadInt();
				OutAnim.FramesPerSecond = Payload.ReadFloat();
				OutAnim.RefPosePath = Payload.ReadFString();
				OutAnim.AdditiveAnimType = static_cast<EAdditiveAnimationType>(Payload.ReadByte());
				OutAnim.RefPoseType = static_cast<EAdditiveBasePoseType>(Payload.ReadByte());
				OutAnim.RefFrameIndex = Payload.ReadInt();
			}
			else if (Name == "TRACKS")
			{
				const int32 Count = Payload.ReadInt();
				OutAnim.Tracks.SetNum(Count);
				for (int32 TrackIndex = 0; TrackIndex < Count; ++TrackIndex)
				{
					FTrack& Track = OutAnim.Tracks[TrackIndex];
					Track.TrackName = Payload.ReadFString();
					ReadVectorKeys(Payload, Track.TrackPosKeys);
					ReadQuatKeys(Payload, Track.TrackRotKeys);
					ReadVectorKeys(Payload, Track.TrackScaleKeys);
				}
			}
			else if (Name == "CURVES")
			{
				const int32 Count = Payload.ReadInt();
				OutAnim.Curves.SetNum(Count);
				for (int32 CurveIndex = 0; CurveIndex < Count; ++CurveIndex)
				{
					FCurve& Curve = OutAnim.Curves[CurveIndex];
					Curve.CurveName = Payload.ReadFString();

					const int32 KeyCount = Payload.ReadInt();
					Curve.CurveKeys.SetNum(KeyCount);
					for (int32 KeyIndex = 0; KeyIndex < KeyCount; ++KeyIndex)
					{
						Curve.CurveKeys[KeyIndex].Frame = Payload.ReadInt();
						Curve.CurveKeys[KeyIndex].FloatValue = Payload.ReadFloat();
					}
				}
			}
			else
			{
				UE_LOG(LogTemp, Warning, TEXT("Unknown anim attribute: %s"), UTF8_TO_TCHAR(Name.c_str()));
			}
		});
	}
}
