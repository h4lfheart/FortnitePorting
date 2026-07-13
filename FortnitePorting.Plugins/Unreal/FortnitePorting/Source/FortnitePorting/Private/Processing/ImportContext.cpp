#include "FortnitePorting/Public/Processing/ImportContext.h"

#include "AutomatedAssetImportData.h"
#include "ComponentReregisterContext.h"
#include "FortnitePorting.h"
#include "Utils.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "Classes/BuildingTextureData.h"
#include "Engine/SkinnedAssetCommon.h"
#include "Engine/StaticMeshActor.h"
#include "Factories/UEFModelFactory.h"
#include "Framework/Notifications/NotificationManager.h"
#include "InterchangeManager.h"
#include "Materials/MaterialInstanceConstant.h"
#include "Processing/Enums.h"
#include "Processing/FortnitePortingTexturePipeline.h"
#include "Processing/MaterialMappings.h"
#include "Processing/Names.h"
#include "TextureCompiler.h"
#include "Components/StaticMeshComponent.h"
#include "Engine/Engine.h"
#include "Materials/Material.h"
#include "Misc/ScopedSlowTask.h"
#include "Serialization/JsonSerializer.h"
#include "Utilities/EditorUtils.h"
#include "Utilities/JsonWrapper.h"
#include "World/BuildingActor.h"

FImportContext::FImportContext(const FJsonWrapper& InMetaData) : MetaData(InMetaData)
{
	EnsureDependencies();
}

void FImportContext::RunExport(const FJsonWrapper& Json)
{
	const auto PrimitiveType = Json.Get<EPrimitiveExportType>("PrimitiveType");
	
	switch (PrimitiveType)
	{
	case EPrimitiveExportType::Mesh:
		ImportMeshData(Json);
		break;
	case EPrimitiveExportType::Texture:
		ImportTextureData(Json);
		break;
	}
}

void FImportContext::RunExportJson(const FString& Data)
{
	TSharedPtr<FJsonObject> JsonObject = MakeShared<FJsonObject>();
	TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Data);
			
	if (!FJsonSerializer::Deserialize(Reader, JsonObject))
	{
		UE_LOG(LogFortnitePorting, Error, TEXT("Unable to deserialize response from FortnitePorting"))
		return;
	}

	const auto Root = FJsonWrapper(JsonObject);
			
	auto Exports = Root.GetArray("Exports");

	FScopedSlowTask ImportTask(Exports.Num(), FText::FromString("Importing Data..."));
	ImportTask.MakeDialog(true);
		
	FSlateNotificationManager::Get().SetAllowNotifications(false);
		
	auto ResponseIndex = 0;
	const FJsonWrapper MetaData = Root["MetaData"];
	for (const auto Export : Exports)
	{
		if (ImportTask.ShouldCancel())
			break;
				
		ResponseIndex++;
			
		FString ExportName = Export.Get<FString>("Name");
				
		ImportTask.DefaultMessage = FText::FromString(FString::Printf(TEXT("Importing Data: %s (%d of %d)"), *ExportName, ResponseIndex, Exports.Num()));
		ImportTask.EnterProgressFrame();
				
		auto ImportContext = FImportContext(MetaData);
		ImportContext.RunExport(Export);
	}
	FSlateNotificationManager::Get().SetAllowNotifications(true);
}

void FImportContext::EnsureDependencies()
{
	if (DefaultMaterial == nullptr)
		DefaultMaterial = Cast<UMaterial>(UEditorAssetLibrary::LoadAsset("/FortnitePorting/Materials/M_FP_Default.M_FP_Default"));
	
	if (LayerMaterial == nullptr)
		LayerMaterial = Cast<UMaterial>(UEditorAssetLibrary::LoadAsset("/FortnitePorting/Materials/M_FP_Layer.M_FP_Layer"));
}

void FImportContext::ImportMeshData(const FJsonWrapper& ExportData)
{
	auto Meshes = ExportData.GetArray("Meshes");
	auto OverrideMeshes = ExportData.GetArray("OverrideMeshes");
	const int32 Count = Meshes.Num() + OverrideMeshes.Num();
	
	FScopedSlowTask ImportTask(Count, FText::FromString("Importing Meshes..."));
	ImportTask.MakeDialog(true);
	
	auto WorldContext = GEngine->GetWorldContextFromGameViewport(GEngine->GameViewport);
	const auto World = WorldContext->World();
	
	auto ExportType = ExportData.Get<EExportType>("Type");
	FString ExportName = ExportData.Get<FString>("Name");
	bool bCreateActor = ExportType == EExportType::World || ExportType == EExportType::Prefab;
	
	int32 MeshIndex = 0;
	auto ImportMeshes = [&](const TArray<FJsonWrapper>& MeshArray)
	{
		for (const auto& Mesh : MeshArray)
		{
			if (ImportTask.ShouldCancel())
				break;
			
			MeshIndex++;
			FString MeshName = Mesh.Get<FString>("Name");
			
			ImportTask.DefaultMessage = FText::FromString(FString::Printf(TEXT("Importing Mesh %d of %d: %s"), MeshIndex, Count, *MeshName));
			ImportTask.EnterProgressFrame();
			
			ImportModel(ExportData, World, nullptr, Mesh, bCreateActor);
		}
	};

	ImportMeshes(Meshes);
	ImportMeshes(OverrideMeshes);
}

void FImportContext::ImportTextureData(const FJsonWrapper& ExportData)
{
	const auto Textures = ExportData.GetArray("Textures");

	// Fire all imports concurrently — Interchange pipelines run on worker threads.
	TArray<UE::Interchange::FAssetImportResultRef> PendingResults;
	PendingResults.Reserve(Textures.Num());

	for (const auto& TextureJson : Textures)
	{
		// Resolve path and skip already-imported / engine assets early.
		const auto PathData = FEditorUtils::GetPathData(TextureJson.Get<FString>("Path"));
		const auto Package = CreatePackage(*PathData.Path);

		if (LoadObject<UTexture>(Package, *PathData.ObjectName) || PathData.RootName.Equals("Engine"))
			continue;

		FString AssetsRoot = MetaData.Get<FString>("AssetsRoot");
		FString TexturePath = FPaths::Combine(AssetsRoot, PathData.Path + ".png");
		if (!FPaths::FileExists(TexturePath))
			TexturePath = FPaths::Combine(AssetsRoot, PathData.Path + ".hdr");
		if (!FPaths::FileExists(TexturePath))
			continue;

		UInterchangeManager& Manager = UInterchangeManager::GetInterchangeManager();
		UInterchangeSourceData* SourceData = Manager.CreateSourceData(TexturePath);

		auto* Pipeline = NewObject<UFortnitePortingTexturePipeline>(GetTransientPackage());
		Pipeline->bWantSRGB = TextureJson.Get<bool>("sRGB");
		Pipeline->WantCompression = TextureJson.Get<TextureCompressionSettings>("CompressionSettings");

		FImportAssetParameters Params;
		Params.bIsAutomated = true;
		Params.bReplaceExisting = false;
		Params.OverridePipelines.Add(Pipeline);

		FString ContentFolder = PathData.Path.LeftChop(PathData.ObjectName.Len() + 1);
		PendingResults.Add(Manager.ImportAssetAsync(ContentFolder, SourceData, Params));
	}

	for (const auto& Result : PendingResults)
	{
		Result->WaitUntilDone();
	}

	// Flush the texture build queue so any subsequent save/cook sees fully compiled assets.
	FTextureCompilingManager::Get().FinishAllCompilation();
}

UObject* FImportContext::ImportModel(const FJsonWrapper& ExportData, UWorld* World, ABuildingActor* Parent, const FJsonWrapper& MeshData, bool bCreateActor)
{
    const auto ImportedObject = ImportMesh(MeshData);

    if (const auto StaticMesh = Cast<UStaticMesh>(ImportedObject); bCreateActor)
    {
        FTransform SpawnTransform;
        auto Actor = World->SpawnActorDeferred<ABuildingActor>(ABuildingActor::StaticClass(), SpawnTransform);
        Actor->Modify();

        Actor->SetActorLabel(*MeshData.Get<FString>("Name"));
        if (Parent) Actor->AttachToActor(Parent, FAttachmentTransformRules(EAttachmentRule::KeepRelative, false));

        Actor->SetActorRelativeLocation(MeshData.Get<FVector>("Location", FVector::ZeroVector));
        Actor->SetActorRelativeRotation(MeshData.Get<FRotator>("Rotation", FRotator::ZeroRotator));
        Actor->SetActorRelativeScale3D(MeshData.Get<FVector>("Scale", FVector::OneVector));

    	for (const auto& TexData : MeshData.GetArray("TextureData"))
    	{
    		Actor->TextureData.Add(FTextureDataInstance {
				.LayerIndex = TexData.Get<int>("Index"),
				.TextureData = ImportBuildingTextureData(TexData)
			});
    	}
    	
        Actor->GetStaticMeshComponent()->ForcedLodModel = 1;
        Actor->GetStaticMeshComponent()->SetStaticMesh(StaticMesh);
        Actor->SetFolderPath(*FString::Printf(TEXT("/%s"), *ExportData.Get<FString>("Name")));
        Actor->FinishSpawning(SpawnTransform);
        Actor->MarkPackageDirty();
    	
    	auto Children = MeshData.GetArray("Children");
    	if (Children.Num() > 0)
    	{
    		FScopedSlowTask ImportTask(Children.Num(), FText::FromString("Importing Children..."));
    		ImportTask.MakeDialog(true);
		
    		int32 ChildIndex = 0;
		
    		for (const auto& Child : Children)
    		{
    			if (ImportTask.ShouldCancel())
    				break;
			
    			ChildIndex++;
    			FString ChildName = Child.Get<FString>("Name");
			
    			ImportTask.DefaultMessage = FText::FromString(FString::Printf(TEXT("Importing Mesh %d of %d: %s"), ChildIndex, Children.Num(), *ChildName));
    			ImportTask.EnterProgressFrame();
    			ImportModel(ExportData, World, Actor, Child, bCreateActor);
    		}
    	}
    }

    return ImportedObject;
}


UObject* FImportContext::ImportMesh(const FJsonWrapper& MeshData)
{
	FString MeshPath = MeshData.Get<FString>("Path");
	auto PathData = FEditorUtils::GetPathData(MeshPath);
	auto Package = CreatePackage(*PathData.Path);
	
	auto Mesh = LoadObject<UObject>(Package, *PathData.ObjectName);
	if (Mesh != nullptr || PathData.RootName.Equals("Engine")) return Mesh;
	
	FString AssetsRoot = MetaData.Get<FString>("AssetsRoot");
	const auto ModelPath = FPaths::Combine(AssetsRoot, PathData.Path + ".uemodel");
	if (!FPaths::FileExists(ModelPath)) return nullptr;

	auto AutomatedData = NewObject<UAutomatedAssetImportData>();
	AutomatedData->bReplaceExisting = false;

	const auto ModelFactory = NewObject<UEFModelFactory>();
	ModelFactory->AutomatedImportData = AutomatedData;

	bool Canceled;
	Mesh = ModelFactory->FactoryCreateFile(nullptr, Package, FName(*PathData.ObjectName), RF_Public | RF_Standalone, ModelPath, nullptr, nullptr, Canceled);

	if (const auto StaticMesh = Cast<UStaticMesh>(Mesh))
	{
		StaticMesh->GetSourceModel(0).BuildSettings.bGenerateLightmapUVs = false;
		StaticMesh->GetSourceModel(0).BuildSettings.bRecomputeNormals = false;
		StaticMesh->GetSourceModel(0).BuildSettings.bRecomputeTangents = false;
		StaticMesh->Modify();
	}
	
	for (const auto& Material : MeshData.GetArray("Materials"))
	{
		int32 Slot = Material.Get<int32>("Slot");
		auto ImportedMaterial = ImportMaterial(Material);
		
		if (const auto StaticMesh = dynamic_cast<UStaticMesh*>(Mesh))
		{
			StaticMesh->SetMaterial(Slot, ImportedMaterial);
		}
		else if (const auto SkeletalMesh = dynamic_cast<USkeletalMesh*>(Mesh))
		{
			SkeletalMesh->GetMaterials()[Slot].MaterialInterface = ImportedMaterial;
		}
	}
	
	return Mesh;
}

UMaterialInstanceConstant* FImportContext::ImportMaterial(const FJsonWrapper& MaterialData)
{
	FString MaterialPath = MaterialData.Get<FString>("Path");
	const auto PathData = FEditorUtils::GetPathData(MaterialPath);
	const auto Package = CreatePackage(*PathData.Path);
	
	auto MaterialInstance = LoadObject<UMaterialInstanceConstant>(Package, *PathData.ObjectName);
	if (MaterialInstance != nullptr || PathData.RootName.Equals("Engine")) return MaterialInstance;
	
	MaterialInstance = NewObject<UMaterialInstanceConstant>(Package, *PathData.ObjectName, RF_Public | RF_Standalone);
	FAssetRegistryModule::AssetCreated(MaterialInstance);
	
	MaterialInstance->PreEditChange(nullptr);

	auto TargetMaterial = DefaultMaterial;
	FMappingCollection TargetMappings = FMaterialMappings::Default;
	
	bool bIsLayerMaterial = false;
	for (const auto& Switch : MaterialData.GetArray("Switches"))
	{
		if (FNames::LayerSwitchNames.Contains(Switch.Get<FString>("Name")))
		{
			bIsLayerMaterial = true;
			break;
		}
	}
	
	if (bIsLayerMaterial)
	{
		for (const auto& Texture : MaterialData.GetArray("Textures"))
		{
			if (FNames::LayerTextureNames.Contains(Texture.Get<FString>("Name")))
			{
				TargetMaterial = LayerMaterial;
				TargetMappings = FMaterialMappings::Layer;
				break;
			}
		}
	}
	
	MaterialInstance->Parent = TargetMaterial;
	MaterialInstance->BlendMode = MaterialData.Get<EBlendMode>("OverrideBlendMode");
	
	for (const auto& TexParam : MaterialData.GetArray("Textures"))
	{
		FString ParamName = TexParam.Get<FString>("Name");
		
		const auto Texture = ImportTexture(TexParam["Texture"]);
		if (Texture == nullptr) continue;
		
		for (const auto& Mapping : TargetMappings.Textures)
		{
			if (Mapping.Name.Equals(ParamName))
			{
				MaterialInstance->SetTextureParameterValueEditorOnly(
					FMaterialParameterInfo(*Mapping.Slot, GlobalParameter), 
					Texture
				);

				if (!Mapping.SwitchSlot.IsEmpty())
				{
					MaterialInstance->SetStaticSwitchParameterValueEditorOnly(
						FMaterialParameterInfo(*Mapping.SwitchSlot, GlobalParameter), 
						true
					);
				}
				break;
			}
		}
	}
	
	for (const auto& Scalar : MaterialData.GetArray("Scalars"))
	{
		FString ParamName = Scalar.Get<FString>("Name");
		float ParamValue = Scalar.Get<float>("Value");
		
		for (const auto& Mapping : TargetMappings.Scalars)
		{
			if (Mapping.Name.Equals(ParamName))
			{
				MaterialInstance->SetScalarParameterValueEditorOnly(
					FMaterialParameterInfo(*Mapping.Slot, GlobalParameter), 
					ParamValue
				);
				break;
			}
		}
	}

	for (const auto& Switch : MaterialData.GetArray("Switches"))
	{
		FString ParamName = Switch.Get<FString>("Name");
		bool ParamValue = Switch.Get<bool>("Value");
		
		for (const auto& Mapping : TargetMappings.Switches)
		{
			if (Mapping.Name.Equals(ParamName))
			{
				MaterialInstance->SetStaticSwitchParameterValueEditorOnly(
					FMaterialParameterInfo(*Mapping.Slot, GlobalParameter), 
					ParamValue
				);
				break;
			}
		}
	}

	MaterialInstance->SetScalarParameterValueEditorOnly(
		FMaterialParameterInfo("Ambient Occlusion", GlobalParameter), 
		MetaData["Settings"].Get<float>("AmbientOcclusion")
	);
	MaterialInstance->SetScalarParameterValueEditorOnly(
		FMaterialParameterInfo("Cavity", GlobalParameter), 
		MetaData["Settings"].Get<float>("Cavity")
	);
	MaterialInstance->SetScalarParameterValueEditorOnly(
		FMaterialParameterInfo("Subsurface", GlobalParameter), 
		MetaData["Settings"].Get<float>("Subsurface")
	);
	
	MaterialInstance->PostEditChange();
	Package->FullyLoad();
	
	FGlobalComponentReregisterContext RecreateComponents;
	
	return MaterialInstance;
}

UBuildingTextureData* FImportContext::ImportBuildingTextureData(const FJsonWrapper& TexData)
{
	const auto PathData = FEditorUtils::GetPathData(TexData.Get<FString>("Path"));
	const auto Package = CreatePackage(*PathData.Path);
	
	auto TextureData = LoadObject<UBuildingTextureData>(Package, *PathData.ObjectName);
	if (TextureData != nullptr || PathData.RootName.Equals("Engine")) return TextureData;
	
	TextureData = NewObject<UBuildingTextureData>(
		Package,
		UBuildingTextureData::StaticClass(),
		*PathData.ObjectName,
		RF_Public | RF_Standalone
	);
	
	TextureData->MarkPackageDirty();
	FAssetRegistryModule::AssetCreated(TextureData);
	
	TextureData->Diffuse = ImportTexture(TexData["Diffuse"]);
	TextureData->Normal = ImportTexture(TexData["Normal"]);
	TextureData->Specular = ImportTexture(TexData["Specular"]);
	
	if (auto OverrideMat = TexData["OverrideMaterial"]; OverrideMat.IsValid())
	{
		TextureData->OverrideMaterial = ImportMaterial(OverrideMat);
	}
	
	return TextureData;
}

UTexture* FImportContext::ImportTexture(const FJsonWrapper& TextureData)
{
	const auto PathData = FEditorUtils::GetPathData(TextureData.Get<FString>("Path"));
	const auto Package = CreatePackage(*PathData.Path);

	auto Texture = LoadObject<UTexture>(Package, *PathData.ObjectName);
	if (Texture != nullptr || PathData.RootName.Equals("Engine")) return Texture;

	FString AssetsRoot = MetaData.Get<FString>("AssetsRoot");
	FString TexturePath = FPaths::Combine(AssetsRoot, PathData.Path + ".png");
	if (!FPaths::FileExists(TexturePath))
		TexturePath = FPaths::Combine(AssetsRoot, PathData.Path + ".hdr");
	if (!FPaths::FileExists(TexturePath))
		return nullptr;

	UInterchangeManager& Manager = UInterchangeManager::GetInterchangeManager();
	UInterchangeSourceData* SourceData = Manager.CreateSourceData(TexturePath);

	auto* Pipeline = NewObject<UFortnitePortingTexturePipeline>(GetTransientPackage());
	Pipeline->bWantSRGB = TextureData.Get<bool>("sRGB");
	Pipeline->WantCompression = TextureData.Get<TextureCompressionSettings>("CompressionSettings");

	FImportAssetParameters Params;
	Params.bIsAutomated = true;
	Params.bReplaceExisting = false;
	Params.OverridePipelines.Add(Pipeline);

	FString ContentFolder = PathData.Path.LeftChop(PathData.ObjectName.Len() + 1);
	UE::Interchange::FAssetImportResultRef ImportResult =
		Manager.ImportAssetAsync(ContentFolder, SourceData, Params);

	ImportResult->WaitUntilDone();
	if (!ImportResult->IsValid())
		return nullptr;

	Texture = Cast<UTexture>(ImportResult->GetFirstAssetOfClass(UTexture::StaticClass()));
	if (Texture == nullptr)
		return nullptr;

	Package->MarkPackageDirty();
	Package->FullyLoad();
	return Texture;
}
