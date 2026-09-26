#pragma once
#include "Interfaces/IPv4/IPv4Endpoint.h"

enum class EMessageCommandType : uint8
{
	Message,
	Data
};

#pragma pack(push, 1)
struct FMessageHeader
{
	EMessageCommandType Type;
	uint32 DataSize;
};
#pragma pack(pop)

class FListenServer : public FRunnable
{
public:
	FListenServer();
	virtual bool Init() override;
	virtual uint32 Run() override;
	virtual void Stop() override;

private:
	void HandleClient(FSocket* ClientSocket);
	
	static bool Recieve(FSocket* ClientSocket, TArray<uint8>& OutData, int32 Size);
	
	bool bIsRunning = false;
	FIPv4Endpoint Endpoint;
	FRunnableThread* Thread;
	FSocket* Socket;
	TQueue<FString, EQueueMode::Mpsc> DataQueue;
};
