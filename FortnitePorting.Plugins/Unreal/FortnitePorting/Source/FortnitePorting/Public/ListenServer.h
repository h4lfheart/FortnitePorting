#pragma once
#include "HttpServerModule.h"

class FListenServer
{
public:
	int Port = 20001;
	FString DataPath = TEXT("/fortnite-porting/data");
	FString PingPath = TEXT("/fortnite-porting/ping");
	
	FHttpServerModule& Module = FHttpServerModule::Get();
	
	void Start() const;
	void Shutdown() const;
};
