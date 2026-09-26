#define LOCTEXT_NAMESPACE "FFortnitePortingModule"
#include "FortnitePorting.h"

#include "Classes/BuildingTextureData.h"
#include "Renderers/BuildingTextureDataThumbnailRenderer.h"
#include "ThumbnailRendering/ThumbnailManager.h"

DEFINE_LOG_CATEGORY(LogFortnitePorting);

void FFortnitePortingModule::StartupModule()
{
	ListenServer = new FListenServer();
	
	UThumbnailManager::Get().RegisterCustomRenderer(
		UBuildingTextureData::StaticClass(), 
		UBuildingTextureDataThumbnailRenderer::StaticClass()
	);
}

void FFortnitePortingModule::ShutdownModule()
{
	delete ListenServer;
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FFortnitePortingModule, FortnitePorting)