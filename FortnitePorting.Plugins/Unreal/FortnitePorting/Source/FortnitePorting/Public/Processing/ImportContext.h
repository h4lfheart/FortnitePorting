#pragma once
#include "EditorAssetLibrary.h"
#include "Factories/TextureFactory.h"
#include "Factories/UEFModelFactory.h"
#include "Models/ExportData.h"
#include "Models/Types/MeshExport.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "Engine/SCS_Node.h"

class FImportContext
{
public:
	inline static const FString IMPORT_ROOT_FOLDER = "FortniteGame";
	FImportContext(const FExportDataMeta& Meta);
	void Run(const TSharedPtr<FJsonObject>& Json);
	
	static void EnsureDependencies();
	inline static UMaterial* DefaultMaterial;
	inline static UMaterial* LayerMaterial;
	
private:
	FExportDataMeta Meta;
	
	FString WrapPathWithImportRootFolder(const FString& Folder);
	
	void ImportMeshData(const FMeshExport& Export);
	UObject* ImportModel(const FExportMesh& Mesh);
	UObject* ImportMesh(const FString& GamePath);
	
	UBlueprint* ImportActorBlueprint(FString ActorBlueprintAssetPath, TMap<FString, FString> StaticMeshAssetPaths, TMap<FString, FString> SkeletalMeshAssetPaths);
	UMaterialInstanceConstant* ImportMaterial(const FExportMaterial& Material);
	
	UTexture* ImportTexture(const FString& GamePath, bool sRGB, TextureCompressionSettings CompressionSettings);
};
