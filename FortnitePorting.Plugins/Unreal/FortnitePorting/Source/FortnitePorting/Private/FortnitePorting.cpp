// Copyright Epic Games, Inc. All Rights Reserved.

#define LOCTEXT_NAMESPACE "FFortnitePortingModule"
#include "FortnitePorting.h"

DEFINE_LOG_CATEGORY(LogFortnitePorting);

void FFortnitePortingModule::StartupModule()
{
	ListenServer.Start();
}

void FFortnitePortingModule::ShutdownModule()
{
	ListenServer.Shutdown();
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FFortnitePortingModule, FortnitePorting)