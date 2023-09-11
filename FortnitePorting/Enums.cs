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