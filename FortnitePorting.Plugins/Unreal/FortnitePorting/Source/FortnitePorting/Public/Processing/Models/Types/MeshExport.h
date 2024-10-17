#pragma once

#include "BaseExport.h"
#include "Processing/Models/Data/ExportObject.h"
#include "MeshExport.generated.h"

USTRUCT()
struct FMeshExport : public FBaseExport
{
	GENERATED_BODY()

	UPROPERTY()
	TArray<FExportMesh> Meshes;
	
	UPROPERTY()
	TArray<FExportMesh> OverrideMeshes;
};