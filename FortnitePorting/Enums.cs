using System.ComponentModel;
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
    
    [Description("Royal")]
    [Icon(MaterialIconKind.Crown)]
    RoyalPurple,
    
    [Description("Ocean")]
    [Icon(MaterialIconKind.WaterDrop)]
    OceanBlue,
    
    [Description("Dark")]
    [Icon(MaterialIconKind.WeatherNight)]
    Dark,
    
    [Description("Mica")]
    [Icon(MaterialIconKind.CircleOpacity)]
    Mica
}