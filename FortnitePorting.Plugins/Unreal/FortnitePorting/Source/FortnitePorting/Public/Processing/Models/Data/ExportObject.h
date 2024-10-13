#pragma once
#include "ExportMaterial.h"
#include "ExportObject.generated.h"

USTRUCT()
struct FExportObject
{
	GENERATED_BODY()

	UPROPERTY()
	FString Name;

	UPROPERTY()
	FVector Location;

	UPROPERTY()
	FRotator Rotation;

	UPROPERTY()
	FVector Scale;
};

USTRUCT()
struct FExportMesh : public FExportObject
{
	GENERATED_BODY()

	UPROPERTY()
	FString Path;
	
	UPROPERTY()
	bool IsEmpty;

	UPROPERTY()
	TArray<FExportMaterial> Materials;
	
	UPROPERTY()
	TArray<FExportMaterial> OverrideMaterials;

	UPROPERTY()
	TArray<FExportTextureData> TextureData;
};

UENUM()
enum class EFortCustomPartType
{
	Head = 0,
	Body = 1,
	Hat = 2,
	Backpack = 3,
	MiscOrTail = 4,
	Face = 5,
	Gameplay = 6,
	NumTypes = 7
};



USTRUCT()
struct FExportPart : public FExportMesh
{
	GENERATED_BODY()

	UPROPERTY()
	EFortCustomPartType Type;
};
