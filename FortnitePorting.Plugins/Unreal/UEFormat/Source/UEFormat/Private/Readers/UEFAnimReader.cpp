#include "Readers/UEFAnimReader.h"
#include "Misc/Compression.h"
#include "Readers/UEFModelReader.h"

UEFAnimReader::UEFAnimReader(const FString Filename) {
	Ar.open(ToCStr(Filename), std::ios::binary);
}

bool UEFAnimReader::Read() {
	const std::string Magic = ReadString(Ar, GMAGIC.length());
	if (Magic != GMAGIC) return false;

	Header.Identifier = ReadFString(Ar);
	Header.FileVersionBytes = ReadData<std::byte>(Ar);
	Header.ObjectName = ReadFString(Ar);
	Header.IsCompressed = ReadData<bool>(Ar);
	NumFrames = ReadData<int32>(Ar);
	FramesPerSecond = ReadData<float>(Ar);

	ReadArchive(Ar);
	return true;
}

void UEFAnimReader::ReadArchive(std::ifstream& Archive)
{
	while (!Archive.eof()) {
		std::string ChunkName = ReadFString(Archive);
		const int32 ArraySize = ReadData<int32>(Archive);
		const int32 ByteSize = ReadData<int32>(Archive);

		if (ChunkName == "TRACKS")
		{
			Tracks.SetNum(ArraySize);
			for (auto i = 0; i < ArraySize; i++) {
				Tracks[i].TrackName = ReadFString(Archive);

				const int32 PosArraySize = ReadData<int32>(Archive);
				Tracks[i].TrackPosKeys.SetNum(PosArraySize);
				for (auto j = 0; j < PosArraySize; j++) {
					Tracks[i].TrackPosKeys[j].Frame = ReadData<int32>(Archive);
					Tracks[i].TrackPosKeys[j].VectorValue = ReadData<FVector3f>(Archive);
				}

				const int32 RotArraySize = ReadData<int32>(Archive);
				Tracks[i].TrackRotKeys.SetNum(RotArraySize);
				for (auto k = 0; k < RotArraySize; k++) {
					Tracks[i].TrackRotKeys[k].Frame = ReadData<int32>(Archive);
					Tracks[i].TrackRotKeys[k].QuatValue = ReadQuat(Archive);
				}

				const int32 ScaleArraySize = ReadData<int32>(Archive);
				Tracks[i].TrackScaleKeys.SetNum(ScaleArraySize);
				for (auto l = 0; l < ScaleArraySize; l++) {
					Tracks[i].TrackScaleKeys[l].Frame = ReadData<int32>(Archive);
					Tracks[i].TrackScaleKeys[l].VectorValue = ReadData<FVector3f>(Archive);
				}
			}
		}
		else if (ChunkName == "CURVES")
		{
			Curves.SetNum(ArraySize);
			for (auto i = 0; i < ArraySize; i++) {
				Curves[i].CurveName = ReadFString(Archive);
				const int32 KeyArraySize = ReadData<int32>(Archive);
				Curves[i].CurveKeys.SetNum(KeyArraySize);
				for (auto j = 0; j < KeyArraySize; j++) {
					Curves[i].CurveKeys[j].Frame = ReadData<int32>(Archive);
					Curves[i].CurveKeys[j].FloatValue = ReadData<float>(Archive);
				}
			}
		}
		else {
			Archive.ignore(ByteSize);
		}
	}
	Archive.close();
}

