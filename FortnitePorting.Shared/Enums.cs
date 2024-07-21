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
    
    [Description("Rocket Racing")]
    RocketRacing,
    
    [Description("Lego")]
    Lego,
    
    [Description("Miscellaneous")]
    Misc
}

public enum EExportType
{
    [Description("None")]
    None,
    
    // COSMETIC
    
    [Description("Outfits"), Export(EPrimitiveExportType.Mesh)]
    Outfit,

    [Description("Backpacks"), Export(EPrimitiveExportType.Mesh)]
    Backpack,

    [Description("Pickaxes"), Export(EPrimitiveExportType.Mesh)]
    Pickaxe,

    [Description("Gliders"), Export(EPrimitiveExportType.Mesh)]
    Glider,

    [Description("Pets"), Export(EPrimitiveExportType.Mesh)]
    Pet,

    [Description("Toys"), Export(EPrimitiveExportType.Mesh)]
    Toy,

    [Description("Emoticons"), Export(EPrimitiveExportType.Texture)]
    Emoticon,

    [Description("Sprays"), Export(EPrimitiveExportType.Texture)]
    Spray,

    [Description("Banners"), Export(EPrimitiveExportType.Texture)]
    Banner,

    [Description("Loading Screens"), Export(EPrimitiveExportType.Texture)]
    LoadingScreen,

    [Description("Emotes"), Export(EPrimitiveExportType.Animation)]
    Emote,
    
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
    
    [Description("Lego Outfits"), Export(EPrimitiveExportType.Mesh)]
    LegoOutfit,
    
    [Description("Lego Emote"), Export(EPrimitiveExportType.Animation)]
    LegoEmote,
    
    [Description("Lego Prop"), Export(EPrimitiveExportType.Mesh)]
    LegoProp,
    
    [Description("Lego Wildlife"), Export(EPrimitiveExportType.Mesh)]
    LegoWildlife,
    
    // GENERIC

    [Description("Mesh"), Export(EPrimitiveExportType.Mesh)]
    Mesh,
    
    [Description("World"), Export(EPrimitiveExportType.Mesh)]
    World,
    
    [Description("Texture"), Export(EPrimitiveExportType.Texture)]
    Texture,
    
    [Description("Animation"), Export(EPrimitiveExportType.Animation)]
    Animation,
    
    [Description("Sound"), Export(EPrimitiveExportType.Sound)]
    Sound
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

public enum EImageFormat
{
    [Description("PNG (.png)")]
    PNG,

    [Description("Targa (.tga)")]
    TGA
}

public enum ERadioSoundFormat
{
    [Description("Save as MP3")]
    MP3,

    [Description("Save as WAV")]
    WAV
}