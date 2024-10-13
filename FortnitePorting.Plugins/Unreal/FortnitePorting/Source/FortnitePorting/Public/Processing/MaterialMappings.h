#pragma once
#include "FortnitePorting/Public/Utils.h"

struct FSlotMapping
{
	FString Name;
	FString Slot;
	
	FSlotMapping() { }
	
	FSlotMapping(const FString& Name, const FString& Slot = "")
	{
		this->Name = Name;
		this->Slot = Slot.IsEmpty() ? Name : Slot;
	}
};

struct FMappingCollection
{
	TArray<FSlotMapping> Textures;
	TArray<FSlotMapping> Scalars;
	TArray<FSlotMapping> Vectors;
	TArray<FSlotMapping> Switches;
};

class FMaterialMappings
{
public:
	static const FMappingCollection Default;
	static const FMappingCollection Layer;
};
