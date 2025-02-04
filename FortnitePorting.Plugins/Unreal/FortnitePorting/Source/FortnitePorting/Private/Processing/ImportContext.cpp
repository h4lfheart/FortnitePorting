#include "FortnitePorting/Public/Processing/ImportContext.h"

#include "AutomatedAssetImportData.h"
#include "ComponentReregisterContext.h"
#include "FortnitePorting.h"
#include "Utils.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "Engine/SkinnedAssetCommon.h"
#include "Engine/StaticMeshActor.h"
#include "KismetCompilerModule.h"
#include "Kismet2/KismetEditorUtilities.h"
#include "Factories/TextureFactory.h"
#include "Factories/UEFModelFactory.h"
#include "FortnitePorting/Public/Utils.h"
#include "FortnitePorting/Public/Processing/Models/Data/ExportObject.h"
#include "FortnitePorting/Public/Processing/Models/Types/BaseExport.h"
#include "Materials/MaterialInstanceConstant.h"
#include "Processing/ImportUtils.h"
#include "Processing/MaterialMappings.h"
#include "Processing/Names.h"
#include "Processing/Models/Types/MeshExport.h"
#include "Processing/BlueprintUtils.h"
#include "World/FortPortActor.h"
#include "UObject/SavePackage.h"

struct FExportDataMeta;

FImportContext::FImportContext(const FExportDataMeta& Meta) : Meta(Meta)
{
	EnsureDependencies();
}

void FImportContext::Run(const TSharedPtr<FJsonObject>& Json)
{
	auto Type = FUtils::GetEnum<EPrimitiveExportType>(Json, "PrimitiveType");

	switch (Type)
	{
	case EPrimitiveExportType::Mesh:
		ImportMeshData(FUtils::GetAsStruct<FMeshExport>(Json));
		break;
	case EPrimitiveExportType::Animation:
		break;
	case EPrimitiveExportType::Texture:
		break;
	case EPrimitiveExportType::Sound:
		break;
	}
	
}

FString FImportContext::WrapPathWithImportRootFolder(const FString& Folder)
{
	auto RootName = Folder.RightChop(1);
	RootName = RootName.Left(RootName.Find("/"));

	if (RootName == "Game")
	{
		FString RelativePath;
		Folder.Split(TEXT("/Game/"), nullptr, &RelativePath);
		return "/Game/" + IMPORT_ROOT_FOLDER + "/" + RelativePath;
	}

	return Folder;
}

UBlueprint* FImportContext::ImportActorBlueprint(FString ActorBlueprintAssetPath, TMap<FString, FString> StaticMeshAssetPaths, TMap<FString, FString> SkeletalMeshAssetPaths)
{
	UE_LOG(LogFortnitePorting, Display, TEXT("Creating Actor Blueprint at: %s"),*ActorBlueprintAssetPath);
	UBlueprint * ActorBlueprint = nullptr;
	
	if(StaticLoadObject(UObject::StaticClass(), nullptr, *ActorBlueprintAssetPath) != nullptr)
	{
		UE_LOG(LogFortnitePorting, Error, TEXT("An asset already exists at: %s"),*ActorBlueprintAssetPath);
		return nullptr;
	}

	UPackage * ActorPackage = CreatePackage(*ActorBlueprintAssetPath);
	if (ActorPackage == nullptr){
		UE_LOG(LogFortnitePorting, Error, TEXT("Unable to create package at: %s"),*ActorBlueprintAssetPath);
		return nullptr;
	}

	UClass* ActorBlueprintClass = nullptr;
	UClass* ActorBlueprintGenClass = nullptr;

	FModuleManager::LoadModuleChecked<IKismetCompilerInterface>("KismetCompiler").GetBlueprintTypesForClass(AActor::StaticClass(), ActorBlueprintClass, ActorBlueprintGenClass);
	ActorBlueprint = FKismetEditorUtilities::CreateBlueprint(AActor::StaticClass(), ActorPackage, *FPaths::GetBaseFilename(ActorBlueprintAssetPath), BPTYPE_Normal, ActorBlueprintClass, ActorBlueprintGenClass);
   
	FAssetRegistryModule::AssetCreated(ActorBlueprint);
	ActorBlueprint->MarkPackageDirty();

	const auto BlueprintPathData = FImportUtils::SplitExportPath(ActorBlueprint->GetPathName());
		
	FBlueprintUtils::AddSceneComponentToBlueprint(BlueprintPathData.Path, "Root", "");

	for (const auto& SkeletalMeshAssetPath : SkeletalMeshAssetPaths)
	{
		FString ComponentName = SkeletalMeshAssetPath.Key;
		FString ComponentAssetPath = SkeletalMeshAssetPath.Value;
		const auto SkeletalMeshPathData = FImportUtils::SplitExportPath(ComponentAssetPath);
		FString Path = WrapPathWithImportRootFolder(SkeletalMeshPathData.Path);
		
		FBlueprintUtils::AddSkeletalMeshComponentToBlueprint(ActorBlueprintAssetPath, ComponentName, "Root", Path);
	}

	for (const auto& StaticMeshAssetPath : StaticMeshAssetPaths)
	{
		FString ComponentName = StaticMeshAssetPath.Key;
		FString ComponentAssetPath = StaticMeshAssetPath.Value;
		const auto StaticMeshPathData = FImportUtils::SplitExportPath(ComponentAssetPath);
		FString Path = WrapPathWithImportRootFolder(StaticMeshPathData.Path);

		FBlueprintUtils::AddStaticMeshComponentToBlueprint(ActorBlueprintAssetPath, ComponentName, "Root", Path);
	}

	FKismetEditorUtilities::CompileBlueprint(ActorBlueprint, EBlueprintCompileOptions::SkipGarbageCollection);
	FString PackageFileName = FPackageName::LongPackageNameToFilename(ActorBlueprint->GetPackage()->GetPathName(), FPackageName::GetAssetPackageExtension());
	FSavePackageArgs SaveArgs;
	SaveArgs.TopLevelFlags = RF_Standalone | RF_Public;
	UPackage::SavePackage(ActorBlueprint->GetPackage(), ActorBlueprint, *PackageFileName, SaveArgs);
	
	UE_LOG(LogFortnitePorting, Display, TEXT("Created Actor Blueprint at: %s"),*ActorBlueprintAssetPath);
	
	return ActorBlueprint;
}



void FImportContext::EnsureDependencies()
{
	if (DefaultMaterial == nullptr)
		DefaultMaterial = Cast<UMaterial>(UEditorAssetLibrary::LoadAsset("/FortnitePorting/Materials/M_FP_Default.M_FP_Default"));
	
	if (LayerMaterial == nullptr)
		LayerMaterial = Cast<UMaterial>(UEditorAssetLibrary::LoadAsset("/FortnitePorting/Materials/M_FP_Layer.M_FP_Layer"));
}

void FImportContext::ImportMeshData(const FMeshExport& Export)
{
	
	TMap<FString, FString> StaticMeshAssetPaths;
	TMap<FString, FString> SkeletalMeshAssetPaths;

	FSavePackageArgs SaveArgs;
	SaveArgs.TopLevelFlags = RF_Standalone | RF_Public;

	const auto Count = Export.Meshes.Num() + Export.OverrideMeshes.Num();
	
	FScopedSlowTask ImportTask(Count, FText::FromString("Importing Meshes..."));
	ImportTask.MakeDialog(true);
	
	auto WorldContext = GEngine->GetWorldContextFromGameViewport(GEngine->GameViewport);
	auto World = WorldContext->World();
	
	auto MeshIndex = 0;
	auto ImportMeshes = [&](TArray<FExportMesh> Meshes)
	{
		for (auto Mesh : Meshes)
		{
			if (ImportTask.ShouldCancel())
            	break;
			
			MeshIndex++;
			
			ImportTask.DefaultMessage = FText::FromString(FString::Printf(TEXT("Importing Mesh %d of %d: %s"), MeshIndex, Count, *Mesh.Name));
			ImportTask.EnterProgressFrame();

			const auto ImportedModel = ImportModel(Mesh);
			
			if (const auto StaticMesh = Cast<UStaticMesh>(ImportedModel); Export.Type == EExportType::World
																	   || Export.Type == EExportType::Prefab)
			{
				const auto Actor = World->SpawnActor<AFortPortActor>();
				Actor->SetActorLabel(*Mesh.Name);
				Actor->SetActorLocation(Mesh.Location);
				Actor->SetActorRotation(Mesh.Rotation);
				Actor->SetActorScale3D(Mesh.Scale);
				Actor->GetStaticMeshComponent()->ForcedLodModel = 1;
				Actor->GetStaticMeshComponent()->SetStaticMesh(StaticMesh);
				Actor->SetFolderPath(*FString::Printf(TEXT("/%s"), *Export.Name));

				for (auto [Hash, Diffuse, Normal, Specular] : Mesh.TextureData)
				{
					Actor->TextureDatas.Add(FBuildingTextureData {
						.Diffuse = FTextureDataItem(Diffuse.Name,
							ImportTexture(Diffuse.Value,
								Diffuse.sRGB,
								Diffuse.CompressionSettings)),
						
						.Normals = FTextureDataItem(Normal.Name,
							ImportTexture(Normal.Value,
								Normal.sRGB,
								Normal.CompressionSettings)),
						
						.Specular = FTextureDataItem(Specular.Name,
							ImportTexture(Specular.Value,
								Specular.sRGB,
								Specular.CompressionSettings))
					});
				}

				Actor->OnConstruction(Actor->GetTransform());
			}

			UStaticMesh * StaticMesh = Cast<UStaticMesh>(ImportedModel);
			USkeletalMesh * SkeletalMesh = Cast<USkeletalMesh>(ImportedModel);
			USkeleton * Skeleton = nullptr;

			if (StaticMesh != nullptr)
			{
				StaticMesh->PostEditChange();
				StaticMesh->GetPackage()->MarkPackageDirty();
				
				FString StaticMeshPackageFileName = FPackageName::LongPackageNameToFilename(StaticMesh->GetPackage()->GetPathName(), FPackageName::GetAssetPackageExtension());
				UPackage::SavePackage(StaticMesh->GetPackage(), StaticMesh, *StaticMeshPackageFileName, SaveArgs);

				// FString Path = WrapPathWithImportRootFolder(Mesh.Path);
				if(!StaticMeshAssetPaths.Contains(Mesh.Name))
				{
					FImportUtils::InsertUniqueKeyToFStringFStringMap(StaticMeshAssetPaths, Mesh.Name, Mesh.Path);
				}
			}
			else if (SkeletalMesh != nullptr)
			{
				Skeleton = SkeletalMesh->GetSkeleton();

				if (Skeleton != nullptr)
				{
					Skeleton->PostEditChange();
					Skeleton->GetPackage()->MarkPackageDirty();
					
					FString SkeletonPackageFileName = FPackageName::LongPackageNameToFilename(Skeleton->GetPackage()->GetPathName(), FPackageName::GetAssetPackageExtension());
					UPackage::SavePackage(Skeleton->GetPackage(), Skeleton, *SkeletonPackageFileName, SaveArgs);
				}
				
				SkeletalMesh->PostEditChange();
				SkeletalMesh->GetPackage()->MarkPackageDirty();
				
				FString SkeletalMeshPackageFileName = FPackageName::LongPackageNameToFilename(SkeletalMesh->GetPackage()->GetPathName(), FPackageName::GetAssetPackageExtension());
				UPackage::SavePackage(SkeletalMesh->GetPackage(), SkeletalMesh, *SkeletalMeshPackageFileName, SaveArgs);

				// FString Path = WrapPathWithImportRootFolder(Mesh.Path);

				if(!SkeletalMeshAssetPaths.Contains(Mesh.Name))
				{
					FImportUtils::InsertUniqueKeyToFStringFStringMap(SkeletalMeshAssetPaths, Mesh.Name, Mesh.Path);
				}
				
			}
		}
	};

	ImportMeshes(Export.Meshes);
	ImportMeshes(Export.OverrideMeshes);

	FString ImportAssetPath = "/Game/" + IMPORT_ROOT_FOLDER + "/Blueprints";
	FString SanitizedName = Export.Name.Replace(TEXT(" "),TEXT(""), ESearchCase::CaseSensitive).
		Replace(TEXT("'"),TEXT(""), ESearchCase::CaseSensitive).
		Replace(TEXT("\""),TEXT(""), ESearchCase::CaseSensitive);
	FString ActorBlueprintName = "BP_" + SanitizedName;
	FString ActorBlueprintAssetPath = FPaths::Combine(ImportAssetPath, ActorBlueprintName);
	UBlueprint* ActorBlueprint = ImportActorBlueprint(ActorBlueprintAssetPath, StaticMeshAssetPaths, SkeletalMeshAssetPaths);
	
	if (ActorBlueprint != nullptr)
	{
		TArray<FAssetData> Objects;
		Objects.Add(ActorBlueprint);
		GEditor->SyncBrowserToObjects(Objects);	
	}
	
}

UObject* FImportContext::ImportModel(const FExportMesh& Mesh)
{
	const auto ImportedObject = ImportMesh(Mesh.Path);

	for (auto Material : Mesh.Materials)
	{
		auto ImportedMaterial = ImportMaterial(Material);
		if (const auto StaticMesh = dynamic_cast<UStaticMesh*>(ImportedObject))
		{
			StaticMesh->SetMaterial(Material.Slot, ImportedMaterial);
		}
		else if (const auto SkeletalMesh = dynamic_cast<USkeletalMesh*>(ImportedObject))
		{
			SkeletalMesh->GetMaterials()[Material.Slot].MaterialInterface = ImportedMaterial;
		}
	}
	
	return ImportedObject;
}

UObject* FImportContext::ImportMesh(const FString& GamePath)
{
	auto PathData = FImportUtils::SplitExportPath(GamePath);
	auto Package = CreatePackage(*WrapPathWithImportRootFolder(PathData.Path));
	
	auto Mesh = LoadObject<UObject>(Package, *PathData.ObjectName);
	if (Mesh != nullptr || PathData.RootName.Equals("Engine")) return Mesh;
		
	const auto MeshPath = FPaths::Combine(Meta.AssetsRoot, PathData.Path + ".uemodel");
	if (!FPaths::FileExists(MeshPath)) return nullptr;

	auto AutomatedData = NewObject<UAutomatedAssetImportData>();
	AutomatedData->bReplaceExisting = false;

	const auto ModelFactory = NewObject<UEFModelFactory>();
	ModelFactory->AutomatedImportData = AutomatedData;

	bool Canceled;
	Mesh = ModelFactory->FactoryCreateFile(nullptr, Package, FName(*PathData.ObjectName), RF_Public | RF_Standalone, MeshPath, nullptr, nullptr, Canceled);

	if (const auto StaticMesh = Cast<UStaticMesh>(Mesh))
	{
		StaticMesh->GetSourceModel(0).BuildSettings.bGenerateLightmapUVs = false;
		StaticMesh->GetSourceModel(0).BuildSettings.bRecomputeNormals = false;
		StaticMesh->GetSourceModel(0).BuildSettings.bRecomputeTangents = false;
		StaticMesh->Modify();
	}
	
	return Mesh;
}

UMaterialInstanceConstant* FImportContext::ImportMaterial(const FExportMaterial& Material)
{
	const auto PathData = FImportUtils::SplitExportPath(Material.Path);
	const auto Package = CreatePackage(*WrapPathWithImportRootFolder(PathData.Path));
	
	auto MaterialInstance = LoadObject<UMaterialInstanceConstant>(Package, *PathData.ObjectName);
	if (MaterialInstance != nullptr || PathData.RootName.Equals("Engine")) return MaterialInstance;
	
	MaterialInstance = NewObject<UMaterialInstanceConstant>(Package, *PathData.ObjectName, RF_Public | RF_Standalone);
	FAssetRegistryModule::AssetCreated(MaterialInstance);
	
	MaterialInstance->PreEditChange(nullptr);

	auto TargetMaterial = DefaultMaterial;
	FMappingCollection TargetMappings = FMaterialMappings::Default;
	
	if (FUtils::Any<FSwitchParameter>(Material.Switches, [&](FSwitchParameter Item) { return FNames::LayerSwitchNames.Contains(Item.Name); })
		&& FUtils::Any<FTextureParameter>(Material.Textures, [&](FTextureParameter Item) { return FNames::LayerTextureNames.Contains(Item.Name); }))
	{
		TargetMaterial = LayerMaterial;
		TargetMappings = FMaterialMappings::Layer;
	}
	
	MaterialInstance->Parent = TargetMaterial;
	MaterialInstance->BlendMode = Material.OverrideBlendMode;
	
	for (auto TextureParameter : Material.Textures)
	{
		if (const auto Mapping = FUtils::FirstOrNull<FSlotMapping>(TargetMappings.Textures, [&](FSlotMapping Item) { return Item.Name.Equals(TextureParameter.Name); }))
		{
			const auto Texture = ImportTexture(TextureParameter.Value, TextureParameter.sRGB, TextureParameter.CompressionSettings);
			if (Texture == nullptr) continue;
			
			MaterialInstance->SetTextureParameterValueEditorOnly(FMaterialParameterInfo(*Mapping->Slot, GlobalParameter), Texture);

			if (!Mapping->SwitchSlot.IsEmpty())
			{
				MaterialInstance->SetStaticSwitchParameterValueEditorOnly(FMaterialParameterInfo(*Mapping->SwitchSlot, GlobalParameter), true);
			}
		}
	}
	
	for (auto ScalarParameter : Material.Scalars)
	{
		if (const auto Mapping = FUtils::FirstOrNull<FSlotMapping>(TargetMappings.Textures, [&](FSlotMapping Item) { return Item.Name.Equals(ScalarParameter.Name); }))
		{
			MaterialInstance->SetScalarParameterValueEditorOnly(FMaterialParameterInfo(*Mapping->Slot, GlobalParameter), ScalarParameter.Value);
		}
	}

	for (auto SwitchParameter : Material.Switches)
	{
		if (const auto Mapping = FUtils::FirstOrNull<FSlotMapping>(TargetMappings.Switches, [&](FSlotMapping Item) { return Item.Name.Equals(SwitchParameter.Name); }))
		{
			MaterialInstance->SetStaticSwitchParameterValueEditorOnly(FMaterialParameterInfo(*Mapping->Slot, GlobalParameter), SwitchParameter.Value);
		}
	}

	MaterialInstance->SetScalarParameterValueEditorOnly(FMaterialParameterInfo("Ambient Occlusion", GlobalParameter), Meta.Settings.AmbientOcclusion);
	MaterialInstance->SetScalarParameterValueEditorOnly(FMaterialParameterInfo("Cavity", GlobalParameter), Meta.Settings.Cavity);
	MaterialInstance->SetScalarParameterValueEditorOnly(FMaterialParameterInfo("Subsurface", GlobalParameter), Meta.Settings.Subsurface);
	
	MaterialInstance->PostEditChange();

	MaterialInstance->MarkPackageDirty();

	FString PackageFileName = FPackageName::LongPackageNameToFilename(Package->GetPathName(), FPackageName::GetAssetPackageExtension());
	FSavePackageArgs SaveArgs;
	SaveArgs.TopLevelFlags = RF_Standalone | RF_Public;
	UPackage::SavePackage(Package, MaterialInstance, *PackageFileName, SaveArgs);

	Package->FullyLoad();
	
	FGlobalComponentReregisterContext RecreateComponents;
	
	return MaterialInstance;
}

UTexture* FImportContext::ImportTexture(const FString& GamePath, const bool sRGB = false, const TextureCompressionSettings CompressionSettings = TC_Default)
{
	const auto PathData = FImportUtils::SplitExportPath(GamePath);
	const auto Package = CreatePackage(*WrapPathWithImportRootFolder(PathData.Path));

	auto Texture = LoadObject<UTexture>(Package, *PathData.ObjectName);
	if (Texture != nullptr || PathData.RootName.Equals("Engine")) return Texture;
	
	const auto TexturePath = FPaths::Combine(Meta.AssetsRoot, PathData.Path + ".png");
	if (!FPaths::FileExists(TexturePath)) return nullptr;

	auto AutomatedData = NewObject<UAutomatedAssetImportData>();
	AutomatedData->bReplaceExisting = false;
	
	const auto TextureFactory = NewObject<UTextureFactory>();
	TextureFactory->NoCompression = false;
	TextureFactory->AutomatedImportData = AutomatedData;
	
	bool Canceled;
	const auto ImportedTexture = TextureFactory->FactoryCreateFile(UTexture::StaticClass(), Package, FName(*PathData.ObjectName), RF_Public | RF_Standalone, TexturePath, nullptr, GWarn, Canceled);
	if (ImportedTexture == nullptr) return nullptr;

	Texture = Cast<UTexture>(ImportedTexture);
	FAssetRegistryModule::AssetCreated(Texture);
		
	Texture->PreEditChange(nullptr);
	Texture->SRGB = sRGB;
	Texture->CompressionSettings = CompressionSettings;
	Texture->PostEditChange();

	Texture->MarkPackageDirty();
	FString PackageFileName = FPackageName::LongPackageNameToFilename(Texture->GetPackage()->GetPathName(), FPackageName::GetAssetPackageExtension());
	FSavePackageArgs SaveArgs;
	SaveArgs.TopLevelFlags = RF_Standalone | RF_Public;
	UPackage::SavePackage(Texture->GetPackage(), Texture, *PackageFileName, SaveArgs);

	Package->FullyLoad();
	return Texture;
}

