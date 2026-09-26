#pragma once

#include "CoreMinimal.h"
#include "ListenServer.h"
#include "Modules/ModuleManager.h"

DECLARE_LOG_CATEGORY_EXTERN(LogFortnitePorting, Log, All);

class FFortnitePortingModule : public IModuleInterface
{
public:

	FListenServer* ListenServer;
	
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
};
