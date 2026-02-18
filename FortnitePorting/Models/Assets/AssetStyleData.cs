using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Clipboard;

namespace FortnitePorting.Models.Assets;

public abstract partial class BaseStyleData : ObservableObject
{
    [ObservableProperty] private string _styleName;
    [ObservableProperty] private Bitmap? _styleDisplayImage;
    [ObservableProperty] private bool _showName = true;
    
    [RelayCommand]
    public virtual async Task CopyIcon()
    {
        await AvaloniaClipboard.SetImageAsync(StyleDisplayImage);
    }
    
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
    
    public AssetStyleData(string name, FStructFallback styleData, Bitmap previewImage)
    {
        StyleData = styleData;
        StyleName = name;
        StyleDisplayImage = previewImage;
    }
}

public partial class AssetColorStyleData : AssetStyleData
{
    [ObservableProperty] private FStructFallback _colorData;
    [ObservableProperty] private bool _isParamSet;
    
    public AssetColorStyleData(string name, FStructFallback styleData, FStructFallback colorData, Bitmap previewImage, bool isParamSet = false) : base(name, styleData, previewImage)
    {
        ColorData = colorData;
        IsParamSet = isParamSet;
    }
}

public partial class ObjectStyleData : BaseStyleData
{
    [ObservableProperty] private UObject _styleData;
    
    public ObjectStyleData(string name, UObject styleData, Bitmap previewImage)
    {
        ShowName = false;
        StyleData = styleData;
        StyleName = name;
        StyleDisplayImage = previewImage;
    }
}

public partial class AnimStyleData : BaseStyleData
{
    [ObservableProperty] private UObject _styleData;
    
    public AnimStyleData(string name, UObject styleData)
    {
        ShowName = true;
        StyleData = styleData;
        StyleName = name;
        StyleDisplayImage = null;
    }
}
