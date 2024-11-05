namespace FortnitePorting.Models.Assets.Custom;

public partial class CustomAssetInfo : Base.BaseAssetInfo
{
    public new CustomAssetItem Asset
    {
        get => (CustomAssetItem) base.Asset;
        private init => base.Asset = value;
    }
    
    public CustomAssetInfo(CustomAssetItem asset)
    {
        Asset = asset;
    }
    
}