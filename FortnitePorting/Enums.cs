using System;
using System.ComponentModel;
using FortnitePorting.Extensions;
using FortnitePorting.Models;
using Material.Icons;

namespace FortnitePorting;

public enum EFortniteVersion
{
    [Description("Latest (Installed)")]
    [Icon(MaterialIconKind.Folder)]
    LatestInstalled,
    
    [Description("Latest (On-Demand)")]
    [Icon(MaterialIconKind.Download)]
    LatestOnDemand,
    
    [Description("Custom")]
    [Icon(MaterialIconKind.Edit)]
    Custom
    
}

public enum EExportLocation
{
    [Description("Blender")]
    Blender,
    
    [Description("Unreal Engine")]
    Unreal,
    
    [Description("Assets Folder")]
    AssetsFolder,
    
    [Description("Custom Folder")]
    CustomFolder,
    
    [Description("Unity (Not Implemented)")]
    [Disabled]
    Unity,
}

public enum EExportTarget
{
    [Description("Asset")]
    [Icon(MaterialIconKind.File)]
    Asset,
    
    [Description("Properties")]
    [Icon(MaterialIconKind.CodeJson)]
    Properties,
    
    [Description("Raw Data")]
    [Icon(MaterialIconKind.Hexadecimal)]
    RawData,
}

public enum EAssetCategory
{
    [Description("Cosmetics")]
    Cosmetics,
    
    [Description("Creative")]
    Creative,
    
    [Description("Gameplay")]
    Gameplay,
    
    [Description("Festival")]
    Festival,
    
    [Description("Rocket Racing")]
    RocketRacing,
    
    [Description("Lego")]
    Lego,
    
    [Description("Fall Guys")]
    FallGuys,
    
    [Description("Misc")]
    Misc
}

internal static class ExportCategory
{
    internal const int Cosmetic = 1 << 8;
    internal const int Creative = 2 << 8;
    internal const int Gameplay = 3 << 8;
    internal const int Festival = 4 << 8;
    internal const int Lego = 5 << 8;
    internal const int FallGuys = 6 << 8;
    internal const int Generic = 7 << 8;
    internal const int Utility = 8 << 8;
}

public enum EExportType
{
    [Description("None")]
    [NonAsset]
    None = 0,

    // COSMETIC

    [Description("Outfits"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Outfit = ExportCategory.Cosmetic + 1,

    [Description("Character Parts"), Export(EPrimitiveExportType.Mesh)]
    [NonAsset]
    CharacterPart = ExportCategory.Cosmetic + 2,

    [Description("Backpacks"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Backpack = ExportCategory.Cosmetic + 3,

    [Description("Pickaxes"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Pickaxe = ExportCategory.Cosmetic + 4,

    [Description("Gliders"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Glider = ExportCategory.Cosmetic + 5,

    [Description("Pets"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Pet = ExportCategory.Cosmetic + 6,

    [Description("Toys"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Toy = ExportCategory.Cosmetic + 7,

    [Description("Emoticons"), Export(EPrimitiveExportType.Texture)]
    [CosmeticAsset]
    Emoticon = ExportCategory.Cosmetic + 8,

    [Description("Sprays"), Export(EPrimitiveExportType.Texture)]
    [CosmeticAsset]
    Spray = ExportCategory.Cosmetic + 9,

    [Description("Banners"), Export(EPrimitiveExportType.Texture)]
    [CosmeticAsset]
    Banner = ExportCategory.Cosmetic + 10,

    [Description("Loading Screens"), Export(EPrimitiveExportType.Texture)]
    [CosmeticAsset]
    LoadingScreen = ExportCategory.Cosmetic + 11,

    [Description("Emotes"), Export(EPrimitiveExportType.Animation)]
    [CosmeticAsset]
    Emote = ExportCategory.Cosmetic + 12,

    [Description("Sidekicks"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    SideKick = ExportCategory.Cosmetic + 13,

    [Description("Kicks"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Kicks = ExportCategory.Cosmetic + 14,

    // CREATIVE

    [Description("Props"), Export(EPrimitiveExportType.Mesh)]
    Prop = ExportCategory.Creative + 1,

    [Description("Prefabs"), Export(EPrimitiveExportType.Mesh)]
    Prefab = ExportCategory.Creative + 2,

    // GAMEPLAY

    [Description("Items"), Export(EPrimitiveExportType.Mesh)]
    Item = ExportCategory.Gameplay + 1,

    [Description("Resources"), Export(EPrimitiveExportType.Mesh)]
    Resource = ExportCategory.Gameplay + 2,

    [Description("Traps"), Export(EPrimitiveExportType.Mesh)]
    Trap = ExportCategory.Gameplay + 3,

    [Description("Vehicles"), Export(EPrimitiveExportType.Mesh)]
    Vehicle = ExportCategory.Gameplay + 4,

    [Description("Wildlife"), Export(EPrimitiveExportType.Mesh)]
    Wildlife = ExportCategory.Gameplay + 5,

    [Description("Weapon Mods"), Export(EPrimitiveExportType.Mesh)]
    WeaponMod = ExportCategory.Gameplay + 6,

    [Description("Sprites"), Export(EPrimitiveExportType.Mesh)]
    Sprite = ExportCategory.Gameplay + 7,

    // FESTIVAL

    [Description("Guitars"), Export(EPrimitiveExportType.Mesh)]
    FestivalGuitar = ExportCategory.Festival + 1,

    [Description("Basses"), Export(EPrimitiveExportType.Mesh)]
    FestivalBass = ExportCategory.Festival + 2,

    [Description("Keytars"), Export(EPrimitiveExportType.Mesh)]
    FestivalKeytar = ExportCategory.Festival + 3,

    [Description("Drums"), Export(EPrimitiveExportType.Mesh)]
    FestivalDrum = ExportCategory.Festival + 4,

    [Description("Microphones"), Export(EPrimitiveExportType.Mesh)]
    FestivalMic = ExportCategory.Festival + 5,

    // LEGO

    [Description("Outfits"), Export(EPrimitiveExportType.Mesh)]
    [Disabled]
    LegoOutfit = ExportCategory.Lego + 1,

    [Description("Emotes"), Export(EPrimitiveExportType.Animation)]
    [Disabled]
    LegoEmote = ExportCategory.Lego + 2,

    [Description("Props"), Export(EPrimitiveExportType.Mesh)]
    [Disabled]
    LegoProp = ExportCategory.Lego + 3,

    [Description("Wildlife"), Export(EPrimitiveExportType.Mesh)]
    [Disabled]
    LegoWildlife = ExportCategory.Lego + 4,

    // FALL GUYS

    [Description("Outfits"), Export(EPrimitiveExportType.Mesh)]
    FallGuysOutfit = ExportCategory.FallGuys + 1,

    // GENERIC

    [Description("Mesh"), Export(EPrimitiveExportType.Mesh)]
    [NonAsset]
    Mesh = ExportCategory.Generic + 1,

    [Description("World"), Export(EPrimitiveExportType.Mesh)]
    [NonAsset]
    World = ExportCategory.Generic + 2,

    [Description("Texture"), Export(EPrimitiveExportType.Texture)]
    [NonAsset]
    Texture = ExportCategory.Generic + 3,

    [Description("Animation"), Export(EPrimitiveExportType.Animation)]
    [NonAsset]
    Animation = ExportCategory.Generic + 4,

    [Description("Sound"), Export(EPrimitiveExportType.Sound)]
    [NonAsset]
    Sound = ExportCategory.Generic + 5,

    [Description("Font"), Export(EPrimitiveExportType.Font)]
    [NonAsset]
    Font = ExportCategory.Generic + 6,

    [Description("Pose Asset"), Export(EPrimitiveExportType.PoseAsset)]
    [NonAsset]
    PoseAsset = ExportCategory.Generic + 7,

    [Description("Material"), Export(EPrimitiveExportType.Material)]
    [NonAsset]
    Material = ExportCategory.Generic + 8,

    [Description("MaterialInstance"), Export(EPrimitiveExportType.Material)]
    [NonAsset]
    MaterialInstance = ExportCategory.Generic + 9,

    // UTILITY

    [Description("Tasty Rig"), Export(EPrimitiveExportType.TastyRig)]
    [NonAsset]
    TastyRig = ExportCategory.Utility + 1
}

public enum EPrimitiveExportType
{
    [Description("Mesh")]
    Mesh,

    [Description("Animation")]
    Animation,

    [Description("Texture")]
    Texture,
    
    [Description("Sound")]
    Sound,
    
    [Description("Font")]
    Font,
    
    [Description("PoseAsset")]
    PoseAsset,
    
    [Description("Material")]
    Material,
    
    // UTILITY
    [Description("Tasty Rig")]
    TastyRig
}

public enum EAssetSortType
{
    [Description("None")]
    None,
    
    [Description("A-Z")]
    AZ,
    
    [Description("Season")]
    Season,

    [Description("Rarity")]
    Rarity,
    
    [Description("Series")]
    Series
}

public enum EImageFormat
{
    [Description("PNG (.png)")]
    PNG,

    [Description("Targa (.tga)")]
    TGA
}

public enum ESoundFormat
{
    [Description("Wavefront (.wav)")]
    WAV,
    
    [Description("MP3 (.mp3)")]
    MP3,
    
    [Description("OGG (.ogg)")]
    OGG,
    
    [Description("FLAC (.flac)")]
    FLAC
}

[Flags]
public enum EWorldFlags
{
    Actors = 1 << 0,
    WorldPartitionGrids = 1 << 1,
    Landscape = 1 << 2,
    InstancedFoliage = 1 << 3,
    HLODs = 1 << 4,
}

public enum EFileFilterType
{
    All,
    Mesh,
    Skeleton,
    Animation,
    Texture,
    Material,
    PoseAsset,
    Sound,
    Font,
    Map
}

public enum EThemeType
{
    
    [Description("Amethyst")]
    [Icon(MaterialIconKind.DiamondStone)]
    Amethyst,
    
    [Description("Rose")]
    [Icon(MaterialIconKind.Flower)]
    Rose,
    
    [Description("Dark")]
    [Icon(MaterialIconKind.WeatherNight)]
    Dark,
    
    [Description("Royal")]
    [Icon(MaterialIconKind.Crown)]
    RoyalPurple,
    
    [Description("Ocean")]
    [Icon(MaterialIconKind.WaterDrop)]
    OceanBlue,
    
    [Description("Mica")]
    [Icon(MaterialIconKind.CircleOpacity)]
    Mica
}