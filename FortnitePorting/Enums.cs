using System;
using System.ComponentModel;
using FortnitePorting.Extensions;
using FortnitePorting.Models;
using Material.Icons;

namespace FortnitePorting;

public enum EFortniteVersion
{
    [Description("Latest (Installed)")]
    LatestInstalled,
    
    [Description("Latest (On-Demand)")]
    LatestOnDemand,
    
    [Description("Custom")]
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

public enum EExportType
{
    [Description("None")]
    [NonAsset]
    None,
    
    // COSMETIC
    
    [Description("Outfits"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Outfit,
    
    [Description("Character Parts"), Export(EPrimitiveExportType.Mesh)]
    [NonAsset]
    CharacterPart,

    [Description("Backpacks"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Backpack,

    [Description("Pickaxes"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Pickaxe,

    [Description("Gliders"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Glider,

    [Description("Pets"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Pet,

    [Description("Toys"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Toy,

    [Description("Emoticons"), Export(EPrimitiveExportType.Texture)]
    [CosmeticAsset]
    Emoticon,

    [Description("Sprays"), Export(EPrimitiveExportType.Texture)]
    [CosmeticAsset]
    Spray,

    [Description("Banners"), Export(EPrimitiveExportType.Texture)]
    [CosmeticAsset]
    Banner,

    [Description("Loading Screens"), Export(EPrimitiveExportType.Texture)]
    [CosmeticAsset]
    LoadingScreen,

    [Description("Emotes"), Export(EPrimitiveExportType.Animation)]
    [CosmeticAsset]
    Emote,
    
    [Description("Sidekicks"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    SideKick,
    
    [Description("Kicks"), Export(EPrimitiveExportType.Mesh)]
    [CosmeticAsset]
    Kicks,
    
    // CREATIVE

    [Description("Props"), Export(EPrimitiveExportType.Mesh)]
    Prop,

    [Description("Prefabs"), Export(EPrimitiveExportType.Mesh)]
    Prefab,
    
    // GAMEPLAY

    [Description("Items"), Export(EPrimitiveExportType.Mesh)]
    Item,
    
    [Description("Resources"), Export(EPrimitiveExportType.Mesh)]
    Resource,

    [Description("Traps"), Export(EPrimitiveExportType.Mesh)]
    Trap,

    [Description("Vehicles"), Export(EPrimitiveExportType.Mesh)]
    Vehicle,

    [Description("Wildlife"), Export(EPrimitiveExportType.Mesh)]
    Wildlife,
        
    [Description("Weapon Mods"), Export(EPrimitiveExportType.Mesh)]
    WeaponMod,
    
    [Description("Sprites"), Export(EPrimitiveExportType.Mesh)]
    Sprite,
    
    // FESTIVAL
    
    [Description("Guitars"), Export(EPrimitiveExportType.Mesh)]
    FestivalGuitar,
    
    [Description("Basses"), Export(EPrimitiveExportType.Mesh)]
    FestivalBass,
    
    [Description("Keytars"), Export(EPrimitiveExportType.Mesh)]
    FestivalKeytar,
    
    [Description("Drums"), Export(EPrimitiveExportType.Mesh)]
    FestivalDrum,
    
    [Description("Microphones"), Export(EPrimitiveExportType.Mesh)]
    FestivalMic,
    
    // LEGO
    
    [Description("Outfits"), Export(EPrimitiveExportType.Mesh)]
    [Disabled]
    LegoOutfit,
    
    [Description("Emotes"), Export(EPrimitiveExportType.Animation)]
    [Disabled]
    LegoEmote,
    
    [Description("Props"), Export(EPrimitiveExportType.Mesh)]
    [Disabled]
    LegoProp,
    
    [Description("Wildlife"), Export(EPrimitiveExportType.Mesh)]
    [Disabled]
    LegoWildlife,
    
    // FALL GUYS
    
    [Description("Outfits"), Export(EPrimitiveExportType.Mesh)]
    FallGuysOutfit,
    
    // GENERIC

    [Description("Mesh"), Export(EPrimitiveExportType.Mesh)]
    [NonAsset]
    Mesh,
    
    [Description("World"), Export(EPrimitiveExportType.Mesh)]
    [NonAsset]
    World,
    
    [Description("Texture"), Export(EPrimitiveExportType.Texture)]
    [NonAsset]
    Texture,
    
    [Description("Animation"), Export(EPrimitiveExportType.Animation)]
    [NonAsset]
    Animation,
    
    [Description("Sound"), Export(EPrimitiveExportType.Sound)]
    [NonAsset]
    Sound,
    
    [Description("Font"), Export(EPrimitiveExportType.Font)]
    [NonAsset]
    Font,
    
    [Description("Pose Asset"), Export(EPrimitiveExportType.PoseAsset)]
    [NonAsset]
    PoseAsset,
    
    [Description("Material"), Export(EPrimitiveExportType.Material)]
    [NonAsset]
    Material,
    
    [Description("MaterialInstance"), Export(EPrimitiveExportType.Material)]
    [NonAsset]
    MaterialInstance,
    
    // UTILITY
    [Description("Tasty Rig"), Export(EPrimitiveExportType.TastyRig)]
    [NonAsset]
    TastyRig
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
    [Description("Dark")]
    [Icon(MaterialIconKind.WeatherNight)]
    Dark,
    
    [Description("Royal Purple")]
    [Icon(MaterialIconKind.Crown)]
    RoyalPurple,
    
    [Description("Ocean Blue")]
    [Icon(MaterialIconKind.WaterDrop)]
    OceanBlue,
    
    [Description("Mica")]
    [Icon(MaterialIconKind.CircleOpacity)]
    Mica,
}