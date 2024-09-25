using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Assets;

public abstract partial class BaseStyleData : ObservableObject
{
    [ObservableProperty] private string _styleName;
    [ObservableProperty] private Bitmap _styleDisplayImage;
}

public partial class AssetStyleData : BaseStyleData
{
    [ObservableProperty] private FStructFallback _styleData;
    
    public AssetStyleData(FStructFallback styleData, Bitmap previewImage)
    {
        StyleData = styleData;
        
        var name = StyleData.GetOrDefault("VariantName", new FText("Unnamed")).Text.ToLower().TitleCase();
        if (string.IsNullOrWhiteSpace(name)) name = "Unnamed";
        StyleName = name;
        
        StyleDisplayImage = previewImage;
    }
}

public partial class ObjectStyleData : BaseStyleData
{
    [ObservableProperty] private UObject _styleData;
    
    public ObjectStyleData(string name, UObject styleData, Bitmap previewImage)
    {
        StyleData = styleData;
        StyleName = name;
        StyleDisplayImage = previewImage;
    }
}