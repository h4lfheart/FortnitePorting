#include "FortnitePorting/Public/World/BuildingActor.h"

#include "Components/StaticMeshComponent.h"
#include "Engine/StaticMesh.h"
#include "Materials/Material.h"
#include "Materials/MaterialInstanceDynamic.h"

void ResetToMaterialDefault(UMaterialInstanceDynamic* DynMat, const FName ParamName)
{
	if (!DynMat) return;

	UMaterialInterface* BaseMat = DynMat->GetBaseMaterial();
	if (!BaseMat) return;

	UTexture* DefaultTex = nullptr;
	BaseMat->GetTextureParameterValue(ParamName, DefaultTex);
	DynMat->SetTextureParameterValue(ParamName, DefaultTex);
}

void ABuildingActor::OnConstruction(const FTransform& Transform)
{
	Super::OnConstruction(Transform);
	
	const auto Component = GetStaticMeshComponent();
	if (Component->GetStaticMesh() == nullptr) return;
	if (TextureData.Num() == 0) return;
	
	auto TargetMaterial = Component->GetStaticMesh()->GetMaterial(0);
	for (const auto& TextureDataInstance : TextureData)
	{
		if (TextureDataInstance.TextureData == nullptr)
			continue;
		
		if (TextureDataInstance.TextureData->OverrideMaterial != nullptr)
			TargetMaterial = TextureDataInstance.TextureData->OverrideMaterial;
	}
	
	const auto DynamicMaterial = UMaterialInstanceDynamic::Create(TargetMaterial, this);
	Component->SetMaterial(0, DynamicMaterial);

	for (auto& TextureDataInstance : TextureData)
	{
		if (TextureDataInstance.TextureData == nullptr)
			continue;
		
		const int32 Index = TextureDataInstance.LayerIndex;

		const FString TextureSuffix = Index > 0 ? FString::Printf(TEXT("_Texture_%d"), Index + 1) : TEXT("");

		const FString SpecSuffix = Index > 0 ? FString::Printf(TEXT("_%d"), Index + 1) : TEXT("");

		const FName DiffuseParam = *FString::Printf(TEXT("Diffuse%s"), *TextureSuffix);
		const FName NormalParam = *FString::Printf(TEXT("Normals%s"), *TextureSuffix);
		const FName SpecularParam = *FString::Printf(TEXT("SpecularMasks%s"), *SpecSuffix);

		// Diffuse
		if (TextureDataInstance.TextureData->Diffuse)
			DynamicMaterial->SetTextureParameterValue(DiffuseParam, TextureDataInstance.TextureData->Diffuse);
		else
			ResetToMaterialDefault(DynamicMaterial, DiffuseParam);

		// Normal
		if (TextureDataInstance.TextureData->Normal)
			DynamicMaterial->SetTextureParameterValue(NormalParam, TextureDataInstance.TextureData->Normal);
		else
			ResetToMaterialDefault(DynamicMaterial, NormalParam);

		// Specular
		if (TextureDataInstance.TextureData->Specular)
			DynamicMaterial->SetTextureParameterValue(SpecularParam, TextureDataInstance.TextureData->Specular);
		else
			ResetToMaterialDefault(DynamicMaterial, SpecularParam);
	}
}
