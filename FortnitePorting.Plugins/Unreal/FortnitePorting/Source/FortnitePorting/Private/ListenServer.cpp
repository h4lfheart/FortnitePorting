#include "ListenServer.h"
#include "Common/TcpSocketBuilder.h"
#include "FortnitePorting/Public/FortnitePorting.h"
#include "Interfaces/IPv4/IPv4Endpoint.h"
#include "Processing/ImportContext.h"
#include "Utilities/FortnitePortingUtils.h"

FListenServer::FListenServer()
{
	Thread = FRunnableThread::Create(this, TEXT("FortnitePortingListenServer"));
}

bool FListenServer::Init()
{
	FIPv4Endpoint::Parse(TEXT("127.0.0.1:40001"), Endpoint);
	
	Socket = FTcpSocketBuilder(TEXT("FPV4 Listen Socket"))
		.AsBlocking()
		.AsReusable()
		.BoundToEndpoint(Endpoint)
		.Listening(5)
		.Build();
	
	
	bIsRunning = true;
	
	return FRunnable::Init();
}

uint32 FListenServer::Run()
{
	UE_LOG(LogFortnitePorting, Log, TEXT("Running FP V4 Server at %s"), *Endpoint.ToString())
	
	while (bIsRunning)
	{
		bool bHasPendingConnection;
		if (!Socket->HasPendingConnection(bHasPendingConnection) || !bHasPendingConnection)
		{
			FPlatformProcess::Sleep(0.01f);
			continue;
		}
		
		TSharedRef<FInternetAddr> RemoteAddress = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
		FSocket* ClientSocket = Socket->Accept(*RemoteAddress, TEXT("FPV4 Client"));
		
		if (ClientSocket == nullptr)
		{
			UE_LOG(LogFortnitePorting, Warning, TEXT("Failed to accept client connection"));
			continue;
		}
        
		UE_LOG(LogFortnitePorting, Log, TEXT("Client connected from %s"), *RemoteAddress->ToString(true));
        
		AsyncTask(ENamedThreads::AnyBackgroundThreadNormalTask, [this, ClientSocket]()
		{
			HandleClient(ClientSocket);
		});
	}
	
	return 0;
}

void FListenServer::Stop()
{
	FRunnable::Stop();
	
	bIsRunning = false;
	Thread->Kill();
	
	UE_LOG(LogFortnitePorting, Log, TEXT("Shutdown FP V4 Server"))
}

void FListenServer::HandleClient(FSocket* ClientSocket)
{
	const TSharedRef<FInternetAddr> ClientAddress = ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->CreateInternetAddr();
	ClientSocket->GetPeerAddress(*ClientAddress);
	
	while (bIsRunning)
	{
		TArray<uint8> HeaderBytes;
		if (!Recieve(ClientSocket, HeaderBytes, sizeof(FMessageHeader)))
			break;

		const FMessageHeader* Header = reinterpret_cast<FMessageHeader*>(HeaderBytes.GetData());
		
		TArray<uint8> Data;
		if (!Recieve(ClientSocket, Data, Header->DataSize))
			break;
		
		auto ReceivedString = FFortnitePortingUtils::BytesToString(Data);

		switch (Header->Type)
		{
		case EMessageCommandType::Message:
			UE_LOG(LogFortnitePorting, Log, TEXT("%s"), *ReceivedString)
			break;
		case EMessageCommandType::Data:
			AsyncTask(ENamedThreads::GameThread, [ReceivedString]
			{
				FImportContext::RunExportJson(ReceivedString);
			});
			break;
		}
	}
	
	ClientSocket->Close();
	ISocketSubsystem::Get(PLATFORM_SOCKETSUBSYSTEM)->DestroySocket(ClientSocket);
	UE_LOG(LogFortnitePorting, Log, TEXT("Client disconnected: %s"), *ClientAddress->ToString(true));

}

bool FListenServer::Recieve(FSocket* ClientSocket, TArray<uint8>& OutData, int32 Size)
{
	OutData.SetNumUninitialized(Size);
	int32 BytesReceived = 0;
	
	while (BytesReceived < Size)
	{
		int32 BytesRead = 0;
		if (!ClientSocket->Recv(OutData.GetData() + BytesReceived, Size - BytesReceived, BytesRead))
		{
			return false;
		}
		
		if (BytesRead <= 0)
		{
			return false;
		}
		
		BytesReceived += BytesRead;
	}
	
	return true;
}
