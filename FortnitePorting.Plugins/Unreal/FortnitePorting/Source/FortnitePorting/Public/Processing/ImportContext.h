#pragma once
#include "EditorAssetLibrary.h"
#include "Factories/TextureFactory.h"
#include "Factories/UEFModelFactory.h"
#include "Models/ExportData.h"
#include "Models/Types/MeshExport.h"

class FImportContext
{
public:
	FImportContext(const FExportDataMeta& Meta);
	void Run(const TSharedPtr<FJsonObject>& Json);
	
	static void EnsureDependencies();
	inline static UMaterial* DefaultMaterial;
	inline static UMaterial* LayerMaterial;
	
private:
	FExportDataMeta Meta;
	
	void ImportMeshData(const FMeshExport& Export);
	UObject* ImportModel(const FExportMesh& Mesh);
	UObject* ImportMesh(const FString& GamePath);
	
	UMaterialInstanceConstant* ImportMaterial(const FExportMaterial& Material);
	
	UTexture* ImportTexture(const FString& GamePath, bool sRGB, TextureCompressionSettings CompressionSettings);
};
