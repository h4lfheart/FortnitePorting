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
		
		if (TextureDataInstance.TextureData->Diffuse != nullptr)
			DynamicMaterial->SetTextureParameterValue(*TextureDataInstance.DiffuseName, TextureDataInstance.TextureData->Diffuse);
		else 
			ResetToMaterialDefault(DynamicMaterial, *TextureDataInstance.DiffuseName);
			
		if (TextureDataInstance.TextureData->Normal != nullptr)
			DynamicMaterial->SetTextureParameterValue(*TextureDataInstance.NormalsName, TextureDataInstance.TextureData->Normal);
		else 
			ResetToMaterialDefault(DynamicMaterial, *TextureDataInstance.NormalsName);
			
		if (TextureDataInstance.TextureData->Specular != nullptr)
			DynamicMaterial->SetTextureParameterValue(*TextureDataInstance.SpecularName, TextureDataInstance.TextureData->Specular);
		else 
			ResetToMaterialDefault(DynamicMaterial, *TextureDataInstance.SpecularName);
	}
}
