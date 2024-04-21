using System.ComponentModel;

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