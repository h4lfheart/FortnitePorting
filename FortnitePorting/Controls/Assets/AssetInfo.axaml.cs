using System;
using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Models.Assets;
using FortnitePorting.Shared.Extensions;
using SkiaSharp;

namespace FortnitePorting.Controls.Assets;

public partial class AssetInfo : UserControl
{
    public AssetInfoData Data;
    
    public AssetInfo(AssetItem asset)
    {
        InitializeComponent();
        
        Data = new AssetInfoData(asset);
        DataContext = Data;
    }
}

public partial class AssetInfoData : ObservableObject
{
    [ObservableProperty] private AssetItem _asset;
    [ObservableProperty] private ObservableCollection<AssetStyleInfo> _styleInfos = [];
    
    public AssetInfoData(AssetItem asset)
    {
        Asset = asset;
        
        var styles = Asset.CreationData.Object.GetOrDefault("ItemVariants", Array.Empty<UObject>());
        foreach (var style in styles)
        {
            var channel = style.GetOrDefault("VariantChannelName", new FText("Style")).Text.ToLower().TitleCase();
            var optionsName = style.ExportType switch
            {
                "FortCosmeticCharacterPartVariant" => "PartOptions",
                "FortCosmeticMaterialVariant" => "MaterialOptions",
                "FortCosmeticParticleVariant" => "ParticleOptions",
                "FortCosmeticMeshVariant" => "MeshOptions",
                "FortCosmeticGameplayTagVariant" => "GenericTagOptions",
                _ => null
            };

            if (optionsName is null) continue;

            var options = style.Get<FStructFallback[]>(optionsName);
            if (options.Length == 0) continue;

            var styleInfo = new AssetStyleInfo(channel, options, Asset.IconDisplayImage);
            if (styleInfo.StyleDatas.Count == 0) continue;
            
            StyleInfos.Add(styleInfo);
        }
    }
}

public partial class AssetStyleInfo : ObservableObject
{
    [ObservableProperty] private string _channelName;
    [ObservableProperty] private int _selectedStyleIndex;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsVisible))] private ObservableCollection<AssetStyleData> _styleDatas = [];
    public bool IsVisible => StyleDatas.Count > 1;
    
    public AssetStyleInfo(string channelName, FStructFallback[] styles, Bitmap fallbackPreviewImage)
    {
        ChannelName = channelName;
        
        foreach (var style in styles)
        {
            if (style.GetOrDefault<FText?>("VariantName") is not { } variantNameText 
                || variantNameText.Text.Equals("Empty", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var previewBitmap = fallbackPreviewImage;
            if (style.TryGetValue(out UTexture2D previewTexture, "PreviewImage"))
            {
                previewBitmap = previewTexture.Decode()!.ToWriteableBitmap();
            }

            StyleDatas.Add(new AssetStyleData(style, previewBitmap));
        }

        SelectedStyleIndex = 0;
    }
}

public partial class AssetStyleData : ObservableObject
{
    [ObservableProperty] private string _styleName;
    [ObservableProperty] private Bitmap _styleDisplayImage;
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