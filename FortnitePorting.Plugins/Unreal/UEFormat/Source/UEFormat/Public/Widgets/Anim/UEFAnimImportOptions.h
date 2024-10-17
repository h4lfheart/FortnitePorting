// Fill out your copyright notice in the Description page of Project Settings.

#pragma once
#include "CoreMinimal.h"
#include "UEFAnimImportOptions.generated.h"

UCLASS(config = Engine, defaultconfig, transient)
class UEFORMAT_API UEFAnimImportOptions : public UObject
{
	GENERATED_BODY()
public:
	UEFAnimImportOptions();
	UPROPERTY( EditAnywhere, Category = "Import Settings")
	TObjectPtr<USkeleton> Skeleton;
	bool bInitialized;
};
