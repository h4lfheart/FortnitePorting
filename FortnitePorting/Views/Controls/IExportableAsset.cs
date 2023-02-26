using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports;

namespace FortnitePorting.Views.Controls;

public interface IExportableAsset
{
    public UObject Asset { get; set; }
    public string DisplayName { get; set; }
    public string DisplayNameSource { get; set; }
    public string Description { get; set; }

    public BitmapImage FullSource { get; set; }
    public EAssetType Type { get; set; }
}