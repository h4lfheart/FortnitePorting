#pragma once
#include "PluginUtils.h"
#include "Interfaces/IPluginManager.h"

struct FPathData
{
	FString Path;
	FString ObjectName;
	FString Folder;
	FString RootName;
};

class FImportUtils 
{
public:
	static FPathData SplitExportPath(const FString& InStr)
	{
		auto RootName = InStr.RightChop(1);
		RootName = RootName.Left(RootName.Find("/"));
	
		if (!RootName.Equals("Game") && !RootName.Equals("Engine") && IPluginManager::Get().FindPlugin(RootName) == nullptr)
		{
			FPluginUtils::FNewPluginParamsWithDescriptor CreationParams;
			CreationParams.Descriptor.FriendlyName = RootName;
			CreationParams.Descriptor.VersionName = "3.0.0";
			CreationParams.Descriptor.Version = 3;
			CreationParams.Descriptor.Category = "Fortnite Porting";
			CreationParams.Descriptor.CreatedBy = "Fortnite Porting";
			CreationParams.Descriptor.CreatedByURL = "https://github.com/h4lfheart/FortnitePorting";
			CreationParams.Descriptor.Description = RootName + " Content Plugin";
			CreationParams.Descriptor.bCanContainContent = true;

			FPluginUtils::FLoadPluginParams LoadParams;
			LoadParams.bEnablePluginInProject = true;
			LoadParams.bUpdateProjectPluginSearchPath = true;
			LoadParams.bSelectInContentBrowser = true;

			FPluginUtils::CreateAndLoadNewPlugin(RootName, FPaths::ProjectPluginsDir(), CreationParams, LoadParams);
		}
	
		FString Path;
		FString ObjectName;
		InStr.Split(".", &Path, &ObjectName);

		FString Folder;
		FString PackageName;
		Path.Split(TEXT("/"), &Folder, &PackageName, ESearchCase::IgnoreCase, ESearchDir::FromEnd);
	
		return FPathData
		{
			Path,
			ObjectName,
			Folder,
			RootName
		};
	}

	static void InsertUniqueKeyToFStringFStringMap(TMap<FString, FString>& Map, FString Key, FString Value)
	{
		int NextIndex = -1;

		for (const auto& MapItem : Map)
		{
			FString ItemKey = MapItem.Key;
			
			if(ItemKey.Equals(Key))
			{
				if (NextIndex < 0)
				{
					NextIndex = 1;
				}
			}
			else if(ItemKey.StartsWith(*Key, true))
			{
				FString Remainder = ItemKey.Replace(*Key, TEXT(""));
				int CurrentIndex = FCString::Atoi(*Remainder);

				if(CurrentIndex > 0 && CurrentIndex + 1 > NextIndex)
				{
					NextIndex = CurrentIndex + 1;
				}
			}
		}

		if (NextIndex > 0)
		{
			FString NextKey = Key + FString::FromInt(NextIndex);
			Map.Add(NextKey, Value);
		}
		else
		{
			Map.Add(Key, Value);
		}
	}
};
