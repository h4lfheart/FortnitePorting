#pragma once

struct FSlotMapping
{
	FString Name;
	FString Slot;
	FString SwitchSlot;
	
	FSlotMapping() { }
	
	FSlotMapping(const FString& Name, const FString& Slot = "", const FString& SwitchSlot = "")
	{
		this->Name = Name;
		this->Slot = Slot.IsEmpty() ? Name : Slot;
		this-> SwitchSlot = SwitchSlot;
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
