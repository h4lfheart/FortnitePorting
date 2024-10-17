#include "ListenServer.h"
#include "IHttpRouter.h"
#include "FortnitePorting/Public/FortnitePorting.h"
#include "FortnitePorting/Public/Utils.h"
#include "FortnitePorting/Public/Processing/Importer.h"

void FListenServer::Start() const
{
	TSharedPtr<IHttpRouter> Router = Module.GetHttpRouter(Port);

	if (!Router.IsValid())
	{
		UE_LOG(LogFortnitePorting, Log, TEXT("Failed to Start Fortnite Porting Server"))
		return;
	}
	

	Router->BindRoute(FHttpPath(DataPath), EHttpServerRequestVerbs::VERB_POST, FHttpRequestHandler::CreateLambda(
		[](const FHttpServerRequest& Request, const FHttpResultCallback& OnComplete)
	{
		auto ResponseBody = FUtils::BytesToString(Request.Body);
		FImporter::Import(ResponseBody);
		
		return false;
	}));

	Router->BindRoute(FHttpPath(PingPath), EHttpServerRequestVerbs::VERB_GET, FHttpRequestHandler::CreateLambda(
		[](const FHttpServerRequest& Request, const FHttpResultCallback& OnComplete)
	{
		TUniquePtr<FHttpServerResponse> Response = FHttpServerResponse::Create("Pong!", "text");
		OnComplete(MoveTemp(Response));
		return true;
	}));
	
	Module.StartAllListeners();
	
	UE_LOG(LogFortnitePorting, Log, TEXT("Started Fortnite Porting Server"))

}

void FListenServer::Shutdown() const
{
	Module.StopAllListeners();
	
	UE_LOG(LogFortnitePorting, Log, TEXT("Shutdown Fortnite Porting Server"))
}
