using FortnitePorting.Extensions;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Assets.Custom;


public sealed partial class CustomAssetItem : Base.BaseAssetItem
{
    public CustomAsset Asset;
    
    public CustomAssetItem(CustomAsset customAsset, EExportType exportType)
    {
        Asset = customAsset;
        
        CreationData = new CustomAssetItemCreationArgs
        {
            DisplayName = customAsset.Name,
            Description = customAsset.Description,
            ID = $"Custom_{customAsset.Name}",
            ExportType = exportType
        };

        IconDisplayImage = customAsset.IconBitmap.ToWriteableBitmap();
        DisplayImage = CreateDisplayImage(customAsset.IconBitmap).ToWriteableBitmap();
    }
}