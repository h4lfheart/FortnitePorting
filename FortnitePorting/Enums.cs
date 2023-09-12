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
    Outfit
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