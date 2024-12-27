#include "FortnitePorting/Public/Processing/ImportContext.h"

#include "AutomatedAssetImportData.h"
#include "ComponentReregisterContext.h"
#include "FortnitePorting.h"
#include "Utils.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "Engine/SkinnedAssetCommon.h"
#include "Engine/StaticMeshActor.h"
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
#include "World/FortPortActor.h"

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

void FImportContext::EnsureDependencies()
{
	if (DefaultMaterial == nullptr)
		DefaultMaterial = Cast<UMaterial>(UEditorAssetLibrary::LoadAsset("/FortnitePorting/Materials/M_FP_Default.M_FP_Default"));
	
	if (LayerMaterial == nullptr)
		LayerMaterial = Cast<UMaterial>(UEditorAssetLibrary::LoadAsset("/FortnitePorting/Materials/M_FP_Layer.M_FP_Layer"));
}

void FImportContext::ImportMeshData(const FMeshExport& Export)
{
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
		}
	};

	ImportMeshes(Export.Meshes);
	ImportMeshes(Export.OverrideMeshes);
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
	auto Package = CreatePackage(*PathData.Path);
	
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
	const auto Package = CreatePackage(*PathData.Path);
	
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
	Package->FullyLoad();
	
	FGlobalComponentReregisterContext RecreateComponents;
	
	return MaterialInstance;
}

UTexture* FImportContext::ImportTexture(const FString& GamePath, const bool sRGB = false, const TextureCompressionSettings CompressionSettings = TC_Default)
{
	const auto PathData = FImportUtils::SplitExportPath(GamePath);
	const auto Package = CreatePackage(*PathData.Path);

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

	Package->FullyLoad();
	return Texture;
}

