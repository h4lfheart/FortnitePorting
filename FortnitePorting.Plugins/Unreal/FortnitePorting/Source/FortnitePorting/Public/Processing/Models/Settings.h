#pragma once

#include "Settings.generated.h"

UENUM()
enum EImageFormat
{
	PNG,
	TGA
};

UENUM()
enum ESoundFormat
{
	WAV,
	MP3,
	OGG,
	FLAC
};

USTRUCT()
struct FSettings
{
	GENERATED_BODY()

	UPROPERTY()
	bool ImportSockets;

	UPROPERTY()
	bool ImportVirtualBones;
	
	UPROPERTY()
	bool ImportCollision;
	
	UPROPERTY()
	bool ImportLobbyPoses;
	
	UPROPERTY()
	bool UseUEFNMaterial;
	
	UPROPERTY()
	float AmbientOcclusion;
	
	UPROPERTY()
	float Cavity;
	
	UPROPERTY()
	float Subsurface;

	UPROPERTY()
	TEnumAsByte<EImageFormat> ImageFormat;
	
	UPROPERTY()
	bool ImportSounds;
	
	UPROPERTY()
	TEnumAsByte<ESoundFormat> SoundFormat;
	
	UPROPERTY()
	bool ImportInstancedFoliage;
	
};
