// Fill out your copyright notice in the Description page of Project Settings.

#pragma once
#include "CoreMinimal.h"
#include "Engine/SkeletalMesh.h"
#include "Engine/StaticMesh.h"
#include "Factories/Factory.h"
#include "Readers/UEFModelReader.h"
#include "Widgets/SkelMesh/UEFSkelMeshImportOptions.h"
#include "UEFModelFactory.generated.h"

UCLASS(hidecategories=Object)
class UEFORMAT_API UEFModelFactory : public UFactory
{
	GENERATED_UCLASS_BODY()

	UPROPERTY()
	UEFSkelMeshImportOptions* SettingsImporter;
	bool bImport;
	bool bImportAll;

	virtual UObject* FactoryCreateFile(UClass* Class, UObject* Parent, FName Name, EObjectFlags Flags, const FString& Filename, const TCHAR* Params, FFeedbackContext* Warn, bool& bOutOperationCanceled) override;

	UStaticMesh* CreateStaticMesh(FLODData& Data, FName Name, UObject* Parent, EObjectFlags Flags);

	USkeletalMesh* CreateSkeletalMeshFromStatic(FString Name, FSkeletonData& SkeletonData, FLODData& Data, UStaticMesh* Mesh, EObjectFlags Flags);

	USkeleton* CreateSkeleton(FString Name, UPackage* ParentPackage, EObjectFlags Flags, FSkeletonData& Data, FReferenceSkeleton& RefSkeleton, FSkeletalMeshImportData&
	                          SkeletalMeshImportData);
};
