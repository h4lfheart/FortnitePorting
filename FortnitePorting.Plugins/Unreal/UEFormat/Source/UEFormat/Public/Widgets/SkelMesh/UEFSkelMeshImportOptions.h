// Fill out your copyright notice in the Description page of Project Settings.

#pragma once
#include "CoreMinimal.h"
#include "UEFSkelMeshImportOptions.generated.h"

UCLASS(config = Engine, defaultconfig, transient)
class UEFORMAT_API UEFSkelMeshImportOptions : public UObject
{
	GENERATED_BODY()
public:
	UEFSkelMeshImportOptions();
	UPROPERTY( EditAnywhere, Category = "Import Settings")
	TObjectPtr<USkeleton> Skeleton;
	bool bInitialized;
};
