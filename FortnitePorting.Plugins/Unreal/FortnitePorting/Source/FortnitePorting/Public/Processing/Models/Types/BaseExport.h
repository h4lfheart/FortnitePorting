#pragma once
#include "BaseExport.generated.h"

UENUM()
enum class EPrimitiveExportType : uint8
{
	Mesh,
	Animation,
	Texture,
	Sound
};

UENUM()
enum class EExportType : uint8
{
    None,
    
    // COSMETIC
    Outfit,
    Backpack,
    Pickaxe,
    Glider,
    Pet,
    Toy,
    Emoticon,
    Spray,
    Banner,
    LoadingScreen,
    Emote,
    
    // CREATIVE
    Prop,
    Prefab,
    
    // GAMEPLAY
    Item,
    Resource,
    Trap,
    Vehicle,
    Wildlife,
    WeaponMod,
    
    // FESTIVAL
    FestivalGuitar,
    FestivalBass,
    FestivalKeytar,
    FestivalDrum,
    FestivalMic,
    
    // LEGO
    LegoOutfit,
    LegoEmote,
    LegoProp,
    LegoWildlife,
    
    // FALL GUYS
    FallGuysOutfit,
    
    // GENERIC
    Mesh,
    World,
    Texture,
    Animation,
    Sound
};

USTRUCT()
struct FBaseExport
{
	GENERATED_BODY()
	
	UPROPERTY()
	FString Name;
	
	UPROPERTY()
	EExportType Type;
	
	UPROPERTY()
	EPrimitiveExportType PrimitiveType;
};