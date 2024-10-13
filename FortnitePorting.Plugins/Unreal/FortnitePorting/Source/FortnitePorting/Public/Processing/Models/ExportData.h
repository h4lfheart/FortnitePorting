#pragma once
#include "Settings.h"
#include "Types/BaseExport.h"
#include "ExportData.generated.h"

USTRUCT()
struct FExportDataMeta
{
	GENERATED_BODY()
	
	UPROPERTY()
	FString AssetsRoot;

	UPROPERTY()
	FSettings Settings;
};


USTRUCT()
struct FExportData
{
	GENERATED_BODY()
	
	UPROPERTY()
	FExportDataMeta MetaData;
	
	UPROPERTY()
	TArray<FBaseExport> Exports;
};
