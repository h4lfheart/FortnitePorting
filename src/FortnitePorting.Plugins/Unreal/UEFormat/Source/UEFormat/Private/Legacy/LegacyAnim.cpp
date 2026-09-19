#include "Legacy/LegacyAnim.h"
#include "Legacy/Chunks.h"

namespace UEFormat::Legacy
{
	void ReadAnim(FUEFormatReader& Ar, FAnimData& OutAnim)
	{
		ForEachChunk(Ar, [&](const std::string& Name, int32 ArraySize, FUEFormatReader& Payload)
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
				OutAnim.Tracks.SetNum(ArraySize);
				for (int32 TrackIndex = 0; TrackIndex < ArraySize; ++TrackIndex)
				{
					FTrack& Track = OutAnim.Tracks[TrackIndex];
					Track.TrackName = Payload.ReadFString();

					const int32 PosArraySize = Payload.ReadInt();
					Track.TrackPosKeys.SetNum(PosArraySize);
					for (int32 KeyIndex = 0; KeyIndex < PosArraySize; ++KeyIndex)
					{
						Track.TrackPosKeys[KeyIndex].Frame = Payload.ReadInt();
						Track.TrackPosKeys[KeyIndex].VectorValue = Payload.ReadVector();
					}

					const int32 RotArraySize = Payload.ReadInt();
					Track.TrackRotKeys.SetNum(RotArraySize);
					for (int32 KeyIndex = 0; KeyIndex < RotArraySize; ++KeyIndex)
					{
						Track.TrackRotKeys[KeyIndex].Frame = Payload.ReadInt();
						Track.TrackRotKeys[KeyIndex].QuatValue = Payload.ReadQuat();
					}

					const int32 ScaleArraySize = Payload.ReadInt();
					Track.TrackScaleKeys.SetNum(ScaleArraySize);
					for (int32 KeyIndex = 0; KeyIndex < ScaleArraySize; ++KeyIndex)
					{
						Track.TrackScaleKeys[KeyIndex].Frame = Payload.ReadInt();
						Track.TrackScaleKeys[KeyIndex].VectorValue = Payload.ReadVector();
					}
				}
			}
			else if (Name == "CURVES")
			{
				OutAnim.Curves.SetNum(ArraySize);
				for (int32 CurveIndex = 0; CurveIndex < ArraySize; ++CurveIndex)
				{
					FCurve& Curve = OutAnim.Curves[CurveIndex];
					Curve.CurveName = Payload.ReadFString();

					const int32 KeyArraySize = Payload.ReadInt();
					Curve.CurveKeys.SetNum(KeyArraySize);
					for (int32 KeyIndex = 0; KeyIndex < KeyArraySize; ++KeyIndex)
					{
						Curve.CurveKeys[KeyIndex].Frame = Payload.ReadInt();
						Curve.CurveKeys[KeyIndex].FloatValue = Payload.ReadFloat();
					}
				}
			}
		});
	}
}
