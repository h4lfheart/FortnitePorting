#pragma once

namespace ExportCategory
{
	constexpr uint32 Cosmetic = 1 << 8;
	constexpr uint32 Creative = 2 << 8;
	constexpr uint32 Gameplay = 3 << 8;
	constexpr uint32 Festival = 4 << 8;
	constexpr uint32 Lego = 5 << 8;
	constexpr uint32 FallGuys = 6 << 8;
	constexpr uint32 Generic = 7 << 8;
}

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
enum class EExportType : uint32
{
	None = 0,

	// COSMETIC
	Outfit = ExportCategory::Cosmetic + 1,
	CharacterPart = ExportCategory::Cosmetic + 2,
	Backpack = ExportCategory::Cosmetic + 3,
	Pickaxe = ExportCategory::Cosmetic + 4,
	Glider = ExportCategory::Cosmetic + 5,
	Pet = ExportCategory::Cosmetic + 6,
	Toy = ExportCategory::Cosmetic + 7,
	Emoticon = ExportCategory::Cosmetic + 8,
	Spray = ExportCategory::Cosmetic + 9,
	Banner = ExportCategory::Cosmetic + 10,
	LoadingScreen = ExportCategory::Cosmetic + 11,
	Emote = ExportCategory::Cosmetic + 12,
	SideKick = ExportCategory::Cosmetic + 13,
	Kicks = ExportCategory::Cosmetic + 14,

	// CREATIVE
	Prop = ExportCategory::Creative + 1,
	Prefab = ExportCategory::Creative + 2,

	// GAMEPLAY
	Item = ExportCategory::Gameplay + 1,
	Resource = ExportCategory::Gameplay + 2,
	Trap = ExportCategory::Gameplay + 3,
	Vehicle = ExportCategory::Gameplay + 4,
	Wildlife = ExportCategory::Gameplay + 5,
	WeaponMod = ExportCategory::Gameplay + 6,
	Sprite = ExportCategory::Gameplay + 7,

	// FESTIVAL
	FestivalGuitar = ExportCategory::Festival + 1,
	FestivalBass = ExportCategory::Festival + 2,
	FestivalKeytar = ExportCategory::Festival + 3,
	FestivalDrum = ExportCategory::Festival + 4,
	FestivalMic = ExportCategory::Festival + 5,

	// LEGO
	LegoOutfit = ExportCategory::Lego + 1,
	LegoEmote = ExportCategory::Lego + 2,
	LegoProp = ExportCategory::Lego + 3,
	LegoWildlife = ExportCategory::Lego + 4,

	// FALL GUYS
	FallGuysOutfit = ExportCategory::FallGuys + 1,

	// GENERIC
	Mesh = ExportCategory::Generic + 1,
	World = ExportCategory::Generic + 2,
	Texture = ExportCategory::Generic + 3,
	Animation = ExportCategory::Generic + 4,
	Sound = ExportCategory::Generic + 5,
	Font = ExportCategory::Generic + 6,
	PoseAsset = ExportCategory::Generic + 7,
	Material = ExportCategory::Generic + 8,
	MaterialInstance = ExportCategory::Generic + 9,
};
