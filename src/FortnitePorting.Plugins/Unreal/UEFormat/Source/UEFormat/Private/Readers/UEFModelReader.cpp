#include "Readers/UEFModelReader.h"
#include "Archive/UEFormatReader.h"
#include "Deserialize/DeserializeModel.h"
#include "Legacy/LegacyModel.h"
#include "Version/EUEFormatVersion.h"

UEFModelReader::UEFModelReader(const FString InFilename)
	: Filename(InFilename)
{
}

bool UEFModelReader::Read()
{
	FUEFormatReader Body;
	if (!UEFormat::LoadFile(Filename, Header, Body))
	{
		return false;
	}

	if (Header.FileVersion >= EUEFormatVersion::AttributeFormatRestructure)
	{
		UEFormat::Deserialize::ReadModel(Body, LODs, Skeleton);
	}
	else
	{
		UEFormat::Legacy::ReadModel(Body, LODs, Skeleton);
	}

	return true;
}
