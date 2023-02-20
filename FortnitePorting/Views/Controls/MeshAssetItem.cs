using System.Windows;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports;

namespace FortnitePorting.Views.Controls;

public class MeshAssetItem : IExportableAsset
{
    public UObject Asset { get; set; }
    public string DisplayName { get; set; }
    public string DisplayNameSource { get; set; }
    public string Description { get; set; }
    public BitmapImage FullSource { get; set; }
    public EAssetType Type { get; set; }
    public Visibility PreviewImageVisibility { get; set; }

    public MeshAssetItem(UObject asset)
    {
        Asset = asset;
        DisplayName = asset.Name;
        DisplayNameSource = DisplayName;
        Description = asset.ExportType switch
        {
            "Skeleton" => "Skeleton",
            "StaticMesh" => "Static Mesh",
            "SkeletalMesh" => "Skeletal Mesh"
        };

        Type = EAssetType.Mesh;
        PreviewImageVisibility = Visibility.Collapsed;
    }
}