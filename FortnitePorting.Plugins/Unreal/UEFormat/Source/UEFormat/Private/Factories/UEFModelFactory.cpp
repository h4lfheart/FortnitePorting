// Copyright © 2025 Marcel K. All rights reserved.

#include "Factories/UEFModelFactory.h"
#include "StaticMeshAttributes.h"
#include "Engine/StaticMesh.h"
#include "Engine/SkeletalMesh.h"
#include "Engine/SkinnedAssetCommon.h"
#include "AssetRegistry/AssetRegistryModule.h"
#include "SkeletalMeshAttributes.h"
#include "StaticToSkeletalMeshConverter.h"
#include "Engine/SkeletalMeshSocket.h"

class IMeshUtilities;

UEFModelFactory::UEFModelFactory(const FObjectInitializer& ObjectInitializer) : Super(ObjectInitializer)
{
	Formats.Add(TEXT("uemodel; UEMODEL Mesh File"));
	SupportedClass = UObject::StaticClass();
	bCreateNew = false;
	bEditorImport = true;
}

UObject* UEFModelFactory::FactoryCreateFile(UClass* Class, UObject* Parent, FName Name, EObjectFlags Flags, const FString& Filename, const TCHAR* Params, FFeedbackContext* Warn, bool& bOutOperationCanceled)
{
	UEFModelReader Data = UEFModelReader(Filename);
	//empty mesh
	if (!Data.Read() || Data.LODs.Num() == 0)
		return nullptr;

	//skeletal mesh
	if (Data.Skeleton.Bones.Num() > 0)
	{
		USkeletalMesh* SkeletalMesh = CreateSkeletalMesh(Data.LODs, Data.Skeleton, Parent, Name, Flags);

		SkeletalMesh->PostEditChange();
		FAssetRegistryModule::AssetCreated(SkeletalMesh);
		return SkeletalMesh;
	}
	else //static mesh
	{
		UStaticMesh* StaticMesh = CreateStaticMesh(Data.LODs, Parent, Name, Flags);

		StaticMesh->PostEditChange();
		FAssetRegistryModule::AssetCreated(StaticMesh);
		return StaticMesh;
	}
}

void UEFModelFactory::PopulateMeshDescription(FMeshDescription& MeshDesc, FLODData& Data)
{
	// Reserve space
	MeshDesc.ReserveNewVertices(Data.Vertices.Num());
	MeshDesc.ReserveNewVertexInstances(Data.Vertices.Num());
	MeshDesc.ReserveNewPolygons(Data.Indices.Num() / 3);
	MeshDesc.ReserveNewPolygonGroups(Data.Materials.Num());
	MeshDesc.SetNumUVChannels(Data.TextureCoordinates.Num());

	//Vertices
	for (auto i = 0; i < Data.Vertices.Num(); ++i)
		MeshDesc.CreateVertex();

	//Indices
	for (const auto& Index : Data.Indices)
		MeshDesc.CreateVertexInstance(Index);
}

void UEFModelFactory::SetMeshAttributes(FMeshDescription& MeshDesc, FLODData& Data)
{
	FStaticMeshAttributes Attributes(MeshDesc);
	Attributes.Register();

	const auto VertexPositions = Attributes.GetVertexPositions();
	const auto VertexInstanceNormals = Attributes.GetVertexInstanceNormals();
	const auto VertexInstanceTangents = Attributes.GetVertexInstanceTangents();
	const auto VertexInstanceBinormalSigns = Attributes.GetVertexInstanceBinormalSigns();
	const auto VertexInstanceColors = Attributes.GetVertexInstanceColors();
	const auto VertexInstanceUVs = Attributes.GetVertexInstanceUVs();
	VertexInstanceUVs.SetNumChannels(Data.TextureCoordinates.Num());

	//Vertices
	for (auto i = 0; i < Data.Vertices.Num(); ++i)
		VertexPositions.Set(i, Data.Vertices[i]);

	//Indices
	for (auto i = 0; i < Data.Indices.Num(); i++) {
		int Index = Data.Indices[i];

		//Normals
		if (Data.Normals.Num() > 0) {
			VertexInstanceBinormalSigns.Set(i, Data.Normals[Index].X);
			VertexInstanceNormals.Set(i, FVector3f(Data.Normals[Index].Y, Data.Normals[Index].Z, Data.Normals[Index].W));
		}
		//Tangents
		if (Data.Tangents.Num() > 0)
			VertexInstanceTangents.Set(i, Data.Tangents[Index]);

		//Vertex Colors
		if (Data.VertexColors.Num() > 0)
			VertexInstanceColors.Set(i, FVector4f(Data.VertexColors[0].Data[Index]));

		//Texture Coordinates
		for (auto u = 0; u < Data.TextureCoordinates.Num(); u++)
			VertexInstanceUVs.Set(i, u, Data.TextureCoordinates[u][Index]);
	}
}

void UEFModelFactory::CreatePolygonGroups(FMeshDescription& MeshDesc, FLODData& Data)
{
	FStaticMeshAttributes Attributes(MeshDesc);
	Attributes.Register();

	for (const auto& [MatIndex, MatName, FirstIndex, NumFaces] : Data.Materials) {
		FPolygonGroupID PolygonGroup = MeshDesc.CreatePolygonGroup();
		for (auto i = FirstIndex; i < FirstIndex + (NumFaces * 3); i += 3)
			MeshDesc.CreatePolygon(PolygonGroup, { i, i + 1, i + 2 });

		Attributes.GetPolygonGroupMaterialSlotNames()[PolygonGroup] = MatName.c_str();
	}
}

TArray<FStaticMaterial> UEFModelFactory::CreateStaticMaterials(TArray<FMaterialChunk> MaterialInfos)
{
	TArray<FStaticMaterial> Materials;
	for (const auto& MaterialInfo : MaterialInfos)
	{
		FStaticMaterial Material;
		Material.MaterialSlotName = MaterialInfo.Name.c_str();
		Material.ImportedMaterialSlotName = MaterialInfo.Name.c_str();
		Material.MaterialInterface = nullptr;
		Materials.Add(Material);
	}
	return Materials;
}

TArray<FSkeletalMaterial> UEFModelFactory::CreateSkeletalMaterials(TArray<FMaterialChunk> MaterialInfos)
{
	TArray<FSkeletalMaterial> Materials;
	for (const auto& MaterialInfo : MaterialInfos)
	{
		FSkeletalMaterial Material;
		Material.MaterialSlotName = MaterialInfo.Name.c_str();
		Material.ImportedMaterialSlotName = MaterialInfo.Name.c_str();
		Material.MaterialInterface = nullptr;
		Materials.Add(Material);
	}
	return Materials;
}

void UEFModelFactory::ProcessLOD(
	FMeshDescription& MeshDesc,
	FLODData& LODData)
{
	PopulateMeshDescription(MeshDesc, LODData);
	SetMeshAttributes(MeshDesc, LODData);
	CreatePolygonGroups(MeshDesc, LODData);
}

UStaticMesh* UEFModelFactory::CreateStaticMesh(TArray<FLODData>& LODData, UObject* Parent, FName Name, EObjectFlags Flags) {
	UStaticMesh* StaticMesh = NewObject<UStaticMesh>(Parent->GetPackage(), Name, Flags);
	
	//Mesh Descriptions
	TArray<FMeshDescription> MeshDescriptions;
	MeshDescriptions.Reserve(LODData.Num());
	//Pointer array for passing to BuildFromMeshDescriptions
	TArray<const FMeshDescription*> MeshDescriptionPtrs;
	MeshDescriptionPtrs.Reserve(LODData.Num());
	
	for (auto i = 0; i < LODData.Num(); ++i) {
		MeshDescriptions.Emplace(); //Modify the array outside of the loop
		MeshDescriptionPtrs.Add(&MeshDescriptions[i]); //Get the pointer and add it to the pointer array
		ProcessLOD(MeshDescriptions[i], LODData[i]);
	}

	StaticMesh->PostEditChange();
	UStaticMesh::FBuildMeshDescriptionsParams BuildParams;
	StaticMesh->BuildFromMeshDescriptions(MeshDescriptionPtrs, BuildParams);
	StaticMesh->GetStaticMaterials() = CreateStaticMaterials(LODData[0].Materials);
	
	//Mesh LOD Settings
	for (auto i = 0; i < StaticMesh->GetSourceModels().Num(); ++i)
	{
		FStaticMeshSourceModel& SourceModel = StaticMesh->GetSourceModel(i);
		SourceModel.BuildSettings.bRecomputeNormals = false;
		SourceModel.BuildSettings.bRecomputeTangents = false;
		SourceModel.BuildSettings.bRemoveDegenerates = false;
		SourceModel.BuildSettings.bGenerateLightmapUVs = false;
	}
	StaticMesh->Modify();
	
	return StaticMesh;
}

USkeletalMesh* UEFModelFactory::CreateSkeletalMesh(TArray<FLODData>& LODData, FSkeletonData& SkeletonData, UObject* Parent, FName Name, EObjectFlags Flags)
{
	USkeletalMesh* SkeletalMesh = NewObject<USkeletalMesh>(Parent->GetPackage(), Name, Flags);

	//Skeleton
	FReferenceSkeleton RefSkeleton;
	USkeleton* Skeleton = CreateSkeleton(Name.ToString(), Parent, Flags, SkeletonData, RefSkeleton);
	
	//Mesh Descriptions
	TArray<FMeshDescription> MeshDescriptions;
	MeshDescriptions.Reserve(LODData.Num());
	//Pointer array for passing to BuildFromMeshDescriptions
	TArray<const FMeshDescription*> MeshDescriptionPtrs;
	MeshDescriptionPtrs.Reserve(LODData.Num());

	for (auto LodIndex = 0; LodIndex < LODData.Num(); ++LodIndex)
	{
		FLODData& Data = LODData[LodIndex];
		MeshDescriptions.Emplace(); //Modify the array outside of the loop
		MeshDescriptionPtrs.Add(&MeshDescriptions[LodIndex]); //Get the pointer and add it to the pointer array
		ProcessLOD(MeshDescriptions[LodIndex], Data);

		//Skeletal Mesh Attributes
		FSkeletalMeshAttributes SkeletalAttributes(MeshDescriptions[LodIndex]);
		SkeletalAttributes.Register();

		FSkeletalMeshAttributes::FBoneNameAttributesRef BoneNames = SkeletalAttributes.GetBoneNames();
		FSkeletalMeshAttributes::FBoneParentIndexAttributesRef BoneParentIndices = SkeletalAttributes.GetBoneParentIndices();
		FSkeletalMeshAttributes::FBonePoseAttributesRef BonePoses = SkeletalAttributes.GetBonePoses();

		//Bones
		for (auto Index = 0; Index < RefSkeleton.GetRawBoneNum(); ++Index)
		{
			FMeshBoneInfo BoneInfo = RefSkeleton.GetRefBoneInfo()[Index];
			FTransform BoneTransform = RefSkeleton.GetRefBonePose()[Index];

			SkeletalAttributes.CreateBone();
			BoneNames.Set(Index, BoneInfo.Name);
			BoneParentIndices.Set(Index, BoneInfo.ParentIndex);
			BonePoses.Set(Index, BoneTransform);
		}

		//Weights
		FSkinWeightsVertexAttributesRef VertexWeights = SkeletalAttributes.GetVertexSkinWeights();

		TMap<int32, TArray<FWeightChunk>> VertexBoneWeights;
		for (const auto& Weight : Data.Weights)
			VertexBoneWeights.FindOrAdd(Weight.WeightVertexIndex).Emplace(Weight);

		for (const auto& [VertexIndex, Weights] : VertexBoneWeights)
		{
			TArray<UE::AnimationCore::FBoneWeight> BoneWeightsArray;
			BoneWeightsArray.Reserve(Weights.Num());
			for (const auto& Weight : Weights)
				BoneWeightsArray.Emplace(Weight.WeightBoneIndex, Weight.WeightAmount);
			VertexWeights.Set(VertexIndex, UE::AnimationCore::FBoneWeights::Create(BoneWeightsArray));
		}

		//Morpth Targets
		for (const auto& MorphTarget : Data.Morphs)
		{
			FString MorphName = MorphTarget.MorphName.c_str();
			SkeletalAttributes.RegisterMorphTargetAttribute(*MorphName, false);
			TVertexAttributesRef<FVector3f> OriginalVertexMorphPositionDelta = SkeletalAttributes.GetVertexMorphPositionDelta(*MorphName);
			for (const auto& MorphDelta : MorphTarget.MorphDeltas)
				OriginalVertexMorphPositionDelta.Set(MorphDelta.MorphVertexIndex, FVector3f(MorphDelta.MorphPosition.X, -MorphDelta.MorphPosition.Y, MorphDelta.MorphPosition.Z));
		}
	}
	
	TArray<FSkeletalMaterial> SkeletalMaterials = CreateSkeletalMaterials(LODData[0].Materials);
	SkeletalMesh->GetMaterials() = SkeletalMaterials;

    FStaticToSkeletalMeshConverter::InitializeSkeletalMeshFromMeshDescriptions(SkeletalMesh, MeshDescriptionPtrs, SkeletalMaterials, RefSkeleton, false, false);

	SkeletalMesh->SetSkeleton(Skeleton);
	Skeleton->MergeAllBonesToBoneTree(SkeletalMesh);
	Skeleton->SetPreviewMesh(SkeletalMesh);

	Skeleton->PostEditChange();
	FAssetRegistryModule::AssetCreated(Skeleton);
	
    return SkeletalMesh;
}

USkeleton* UEFModelFactory::CreateSkeleton(FString Name, UObject* Parent, EObjectFlags Flags, FSkeletonData& Data, FReferenceSkeleton& RefSkeleton)
{
	FString SkeletonName = Name + "_Skeleton";
	auto SkeletonPackage = CreatePackage(*FPaths::Combine(FPaths::GetPath(Parent->GetPathName()), SkeletonName));
	USkeleton* Skeleton = NewObject<USkeleton>(SkeletonPackage, FName(*SkeletonName), Flags);
	
	FReferenceSkeletonModifier RefSkeletonModifier(RefSkeleton, Skeleton);
	RefSkeleton.Empty();

	for (const auto& Bone : Data.Bones)
	{
		FTransform Transform;
		Transform.SetLocation(FVector(Bone.BonePos));
		Transform.SetRotation(FQuat(Bone.BoneRot));
		Transform.SetScale3D(FVector(1, 1, 1));

		FMeshBoneInfo BoneInfo(Bone.BoneName.c_str(), Bone.BoneName.c_str(), Bone.BoneParentIndex);
		RefSkeletonModifier.Add(BoneInfo, FTransform(Transform));
	}

	for (const auto& Socket : Data.Sockets)
	{
		USkeletalMeshSocket* NewSocket = NewObject<USkeletalMeshSocket>(Skeleton);
		NewSocket->SocketName = Socket.SocketName.c_str();
		NewSocket->BoneName = Socket.SocketParentName.c_str();
		NewSocket->RelativeLocation = FVector(Socket.SocketPos);
		NewSocket->RelativeRotation = FQuat(Socket.SocketRot).Rotator();
		NewSocket->RelativeScale = FVector(Socket.SocketScale);
		Skeleton->Sockets.Add(NewSocket);
	}

	for (const auto& VirtualBone : Data.VirtualBones)
		Skeleton->AddNewNamedVirtualBone(VirtualBone.SourceBoneName.c_str(), VirtualBone.TargetBoneName.c_str(), VirtualBone.VirtualBoneName.c_str());	
	
	return Skeleton;
}