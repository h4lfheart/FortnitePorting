using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.GameplayTags;
using FortnitePorting.Models.Assets.Base;

namespace FortnitePorting.Models.Assets.Asset;

public class AssetItemCreationArgs : BaseAssetItemCreationArgs
{
    public required UObject Object { get; set; }
    public string? LowResIconPath { get; set; }
    public string? HighResIconPath { get; set; }
    public string? IconPath => LowResIconPath ?? HighResIconPath;
    public FGameplayTagContainer? GameplayTags { get; set; }
    
    public bool HideRarity { get; set; } = false;
}