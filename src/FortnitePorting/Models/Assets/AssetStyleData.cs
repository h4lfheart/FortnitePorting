using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Clipboard;
using FortnitePorting.Views;

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
    
    [RelayCommand]
    public virtual async Task CopyPath()
    {
        Info.Message("Unsupported Asset", "Cannot copy the path of this type of asset.");
    }
    
    [RelayCommand]
    public virtual async Task NavigateTo()
    {
        Info.Message("Unsupported Asset", "Cannot navigate to this type of asset.");
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
    [ObservableProperty] private EExportType _associatedExportType = EExportType.None;
    
    public ObjectStyleData(string name, UObject styleData, Bitmap? previewImage)
    {
        ShowName = false;
        StyleData = styleData;
        StyleName = name;
        StyleDisplayImage = previewImage;
    }
    
    public override async Task CopyPath()
    {
        await App.Clipboard.SetTextAsync(StyleData.GetPathName());
    }

    public override async Task NavigateTo()
    {
        Navigation.App.Open<FilesView>();

        var assetPath = UEParse.Provider.FixPath(StyleData.GetPathName().SubstringBefore("."));
        FilesVM.JumpTo(assetPath);
        
        AppWM.Window.BringToTop();
    }
}

public class AnimStyleData : ObjectStyleData
{
    public AnimStyleData(string name, UObject styleData) : base(name, styleData, null)
    {
        ShowName = true;
    }
}