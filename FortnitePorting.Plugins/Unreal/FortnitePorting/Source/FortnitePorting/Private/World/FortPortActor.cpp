#include "FortnitePorting/Public/World/FortPortActor.h"

void AFortPortActor::OnConstruction(const FTransform& Transform)
{
	Super::OnConstruction(Transform);
	
	const auto Component = GetStaticMeshComponent();
	if (Component->GetStaticMesh() == nullptr) return;
	if (TextureDatas.Num() == 0) return;
	
	const auto TargetMaterial = Component->GetStaticMesh()->GetMaterial(0);
	const auto DynamicMaterial = UMaterialInstanceDynamic::Create(TargetMaterial, this);
	Component->SetMaterial(0, DynamicMaterial);

	for (auto TextureData : TextureDatas)
	{
		if (TextureData.Diffuse.Texture != nullptr)
			DynamicMaterial->SetTextureParameterValue(*TextureData.Diffuse.Name, TextureData.Diffuse.Texture);
			
		if (TextureData.Normals.Texture != nullptr)
			DynamicMaterial->SetTextureParameterValue(*TextureData.Normals.Name, TextureData.Normals.Texture);
			
		if (TextureData.Specular.Texture != nullptr)
			DynamicMaterial->SetTextureParameterValue(*TextureData.Specular.Name, TextureData.Specular.Texture);
	}
}
