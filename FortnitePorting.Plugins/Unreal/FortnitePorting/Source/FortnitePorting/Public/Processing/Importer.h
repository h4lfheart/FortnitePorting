#pragma once
#include "ImportContext.h"

class FImporter
{
public:

	static void Import(const FString& Data)
	{
		TSharedPtr<FJsonObject> JsonObject = MakeShared<FJsonObject>();
		TSharedRef<TJsonReader<>> Reader = TJsonReaderFactory<>::Create(Data);
			
		if (!FJsonSerializer::Deserialize(Reader, JsonObject))
		{
			UE_LOG(LogFortnitePorting, Error, TEXT("Unable to deserialize response from FortnitePorting"))
			return;
		}

		FImportContext::EnsureDependencies();
			
		auto Exports = JsonObject->GetArrayField(TEXT("Exports"));
		
		FScopedSlowTask ImportTask(Exports.Num(), FText::FromString("Importing Data..."));
		ImportTask.MakeDialog(true);
		
		auto ResponseIndex = 0;
		const auto Meta = FUtils::GetAsStruct<FExportDataMeta>(JsonObject, "MetaData");
		for (const auto Export : Exports)
		{
			if (ImportTask.ShouldCancel())
				break;
				
			ResponseIndex++;
				
			ImportTask.DefaultMessage = FText::FromString(FString::Printf(TEXT("Importing Data: %s (%d of %d)"), *Export->AsObject()->GetStringField(TEXT("Name")), ResponseIndex, Exports.Num()));
			ImportTask.EnterProgressFrame();
				
			auto ImportContext = FImportContext(Meta);
			ImportContext.Run(Export->AsObject());
		}
		
		
	}
};
