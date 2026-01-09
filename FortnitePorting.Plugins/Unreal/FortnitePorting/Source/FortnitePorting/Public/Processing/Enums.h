#pragma once

UENUM()
enum class EPrimitiveExportType : uint8
{
	Mesh,
	Animation,
	Texture,
	Sound,
	Font,
	PoseAsset,
	Material
};

UENUM()
enum class EExportType : uint8
{
	None,
    
	// COSMETIC
	Outfit,
	CharacterPart,
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
	SideKick,
    
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
	Sound,
	Font,
	PoseAsset,
	Material,
	MaterialInstance
};
