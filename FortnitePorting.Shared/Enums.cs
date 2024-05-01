using System.ComponentModel;
using FortnitePorting.Shared.Models;

namespace FortnitePorting.Shared;

public enum EFortniteVersion
{
    [Description("Latest (Installed)")]
    LatestInstalled,
    
    [Description("Latest (On-Demand)")]
    LatestOnDemand,
    
    [Description("Custom")]
    Custom,
    
    /*[Description("Fortnite v29.10")]
    Custom_29_10,
    
    [Description("Fortnite v29.00")]
    Custom_29_00,
    
    [Description("Fortnite v12.41")]
    Custom_12_41*/
    
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
    CustomFolder
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
}

public enum EAssetType
{
     [Description("None")]
    None,
    
    // COSMETIC
    
    [Description("Outfits"), Export(EExportType.Mesh)]
    Outfit,

    [Description("Backpacks"), Export(EExportType.Mesh)]
    Backpack,

    [Description("Pickaxes"), Export(EExportType.Mesh)]
    Pickaxe,

    [Description("Gliders"), Export(EExportType.Mesh)]
    Glider,

    [Description("Pets"), Export(EExportType.Mesh)]
    Pet,

    [Description("Toys"), Export(EExportType.Mesh)]
    Toy,

    [Description("Emoticons"), Export(EExportType.Texture)]
    Emoticon,

    [Description("Sprays"), Export(EExportType.Texture)]
    Spray,

    [Description("Banners"), Export(EExportType.Texture)]
    Banner,

    [Description("Loading Screens"), Export(EExportType.Texture)]
    LoadingScreen,

    [Description("Emotes"), Export(EExportType.Animation)]
    Emote,
    
    // CREATIVE

    [Description("Props"), Export(EExportType.Mesh)]
    Prop,

    [Description("Prefabs"), Export(EExportType.Mesh)]
    Prefab,
    
    // GAMEPLAY

    [Description("Items"), Export(EExportType.Mesh)]
    Item,
    
    [Description("Resources"), Export(EExportType.Mesh)]
    Resource,

    [Description("Traps"), Export(EExportType.Mesh)]
    Trap,

    [Description("Vehicles"), Export(EExportType.Mesh)]
    Vehicle,

    [Description("Wildlife"), Export(EExportType.Mesh)]
    Wildlife,
        
    [Description("Weapon Mods"), Export(EExportType.Mesh)]
    WeaponMod,
    
    // FESTIVAL
    
    [Description("Guitars"), Export(EExportType.Mesh)]
    FestivalGuitar,
    
    [Description("Basses"), Export(EExportType.Mesh)]
    FestivalBass,
    
    [Description("Keytars"), Export(EExportType.Mesh)]
    FestivalKeytar,
    
    [Description("Drums"), Export(EExportType.Mesh)]
    FestivalDrum,
    
    [Description("Microphones"), Export(EExportType.Mesh)]
    FestivalMic,
    
    // GENERIC

    [Description("Mesh"), Export(EExportType.Mesh)]
    Mesh,
    
    [Description("World"), Export(EExportType.Mesh)]
    World,
    
    [Description("Texture"), Export(EExportType.Texture)]
    Texture,
    
    [Description("Animation"), Export(EExportType.Animation)]
    Animation,
    
    [Description("Sound"), Export(EExportType.Sound)]
    Sound
}

public enum EExportType
{
    [Description("Mesh")]
    Mesh,

    [Description("Animation")]
    Animation,

    [Description("Texture")]
    Texture,
    
    [Description("Sound")]
    Sound
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
    Rarity
}