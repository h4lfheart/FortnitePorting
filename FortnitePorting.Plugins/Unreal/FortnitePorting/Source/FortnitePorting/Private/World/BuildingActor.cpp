#include "FortnitePorting/Public/World/BuildingActor.h"

void ResetToMaterialDefault(UMaterialInstanceDynamic* DynMat, const FString& ParamName)
{
	if (!DynMat) return;

	UMaterialInterface* BaseMat = DynMat->GetBaseMaterial();
	if (!BaseMat) return;

	UTexture* DefaultTex = nullptr;
	BaseMat->GetTextureParameterValue(FName(*ParamName), DefaultTex);
	DynMat->SetTextureParameterValue(FName(*ParamName), DefaultTex);
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
		
		const int32 Index = Instance.LayerIndex;

		const FString TextureSuffix = Index > 0 ? FString::Printf(TEXT("_Texture_%d"), Index + 1) : TEXT("");

		const FString SpecSuffix = Index > 0 ? FString::Printf(TEXT("_%d"), Index + 1) : TEXT("");

		const FName DiffuseParam = *FString::Printf(TEXT("Diffuse%s"), *TextureSuffix);
		const FName NormalParam = *FString::Printf(TEXT("Normals%s"), *TextureSuffix);
		const FName SpecularParam = *FString::Printf(TEXT("SpecularMasks%s"), *SpecSuffix);

		// Diffuse
		if (Instance.TextureData->Diffuse)
			DynamicMaterial->SetTextureParameterValue(DiffuseParam, Instance.TextureData->Diffuse);
		else
			ResetToMaterialDefault(DynamicMaterial, DiffuseParam);

		// Normal
		if (Instance.TextureData->Normal)
			DynamicMaterial->SetTextureParameterValue(NormalParam, Instance.TextureData->Normal);
		else
			ResetToMaterialDefault(DynamicMaterial, NormalParam);

		// Specular
		if (Instance.TextureData->Specular)
			DynamicMaterial->SetTextureParameterValue(SpecularParam, Instance.TextureData->Specular);
		else
			ResetToMaterialDefault(DynamicMaterial, SpecularParam);
	}
}
