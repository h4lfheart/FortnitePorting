// Copyright © 2025 Marcel K. All rights reserved.

#pragma once
#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"

class FUEFormatModule : public IModuleInterface
{
public:
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
};
