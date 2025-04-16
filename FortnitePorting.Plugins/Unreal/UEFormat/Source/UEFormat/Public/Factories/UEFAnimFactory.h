// Copyright © 2025 Marcel K. All rights reserved.

#pragma once
#include "CoreMinimal.h"
#include "Factories/Factory.h"
#include "Widgets/Anim/UEFAnimImportOptions.h"
#include "UEFAnimFactory.generated.h"


UCLASS(hidecategories = Object)
class UEFORMAT_API UEFAnimFactory : public UFactory
{
	GENERATED_UCLASS_BODY()

	UPROPERTY()
	UEFAnimImportOptions* SettingsImporter;
	bool bImport;
	bool bImportAll;

	virtual UObject* FactoryCreateFile(UClass* Class, UObject* Parent, FName Name, EObjectFlags Flags, const FString& Filename, const TCHAR* Params, FFeedbackContext* Warn, bool& bOutOperationCanceled) override;
};