using System.ComponentModel;

namespace FortnitePorting;

public enum ELoadingType
{
    [Description("Local (Installed)")]
    Local,

    [Description("Live (On-Demand)")]
    Live,

    [Description("Custom (Old Versions)")]
    Custom
}

public enum EAssetType
{
    [Description("Outfits")]
    Outfit,
    
    [Description("Back Blings")]
    Backpack,
    
    [Description("Pickaxes")]
    Pickaxe,
    
    [Description("Gliders")]
    Glider,
    
    [Description("Pets")]
    Pet,
    
    [Description("Toys")]
    Toy,
    
    [Description("Sprays")]
    Spray,
    
    [Description("Loading Screens")]
    LoadingScreen,
    
    [Description("Emotes")]
    Emote,
    
    [Description("Music Packs")]
    MusicPack,
    
    [Description("Props")]
    Prop,
    
    [Description("Galleries")]
    Gallery,
    
    [Description("Meshes")]
    Mesh,
    
    [Description("Items")]
    Item,
    
    [Description("Traps")]
    Trap,
    
    [Description("Vehicles")]
    Vehicle,
    
    [Description("Wildlife")]
    Wildlife,
    
}

public enum EExportType
{
    [Description("Blender")]
    Blender,

    [Description("Unreal Engine")]
    Unreal,
    
    [Description("Folder")]
    Folder
}

public enum ESortType
{
    [Description("Default")]
    Default,

    [Description("A-Z")]
    AZ,
    
    [Description("Season")]
    Season,
    
    [Description("Rarity")]
    Rarity
}