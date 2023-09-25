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
    
    [Description("Banners")]
    Banner,
    
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

public enum EImageType
{
    [Description("PNG (.png)")]
    PNG,

    [Description("Targa (.tga)")]
    TGA
}

public enum ERigType
{
    [Description("Default Rig (FK)")]
    Default,

    [Description("Tasty Rig (IK)")]
    Tasty
}

public enum ESupportedLODs
{
    [Description("LOD 0")]
    LOD0,
    
    [Description("LOD 1")]
    LOD1,
    
    [Description("LOD 2")]
    LOD2,
    
    [Description("LOD 3")]
    LOD3,
    
    [Description("LOD 4")]
    LOD4
}