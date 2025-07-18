using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Extensions;
using FortnitePorting.Shared.Extensions;
using SkiaSharp;
using AssetLoader = FortnitePorting.Models.Assets.Loading.AssetLoader;
using SkiaExtensions = FortnitePorting.Extensions.SkiaExtensions;

namespace FortnitePorting.Models.Assets;

public partial class AssetStyleInfo : ObservableObject
{
    [ObservableProperty] private string _channelName;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsVisible))] private ObservableCollection<BaseStyleData> _styleDatas = [];
    public bool IsVisible => StyleDatas.Count > 1;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(SelectionMode))] private bool _multiSelect = false;
    public SelectionMode SelectionMode => MultiSelect ? SelectionMode.Multiple : SelectionMode.Single;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(SelectedStyle))] private int _selectedStyleIndex;
    [ObservableProperty] private ObservableCollection<BaseStyleData> _selectedItems = [];

    public BaseStyleData SelectedStyle => StyleDatas[SelectedStyleIndex];
    
    
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
    
    public AssetStyleInfo(string channelName, IEnumerable<UObject> styles, Bitmap fallbackPreviewImage)
    {
        MultiSelect = true;
        ChannelName = channelName;
        
        foreach (var style in styles)
        {
            var previewBitmap = fallbackPreviewImage;
            if (AssetLoader.GetIcon(style)?.Decode()?.ToSkBitmap() is { } iconBitmap)
            {
                var rarity = style.GetOrDefault("Rarity", EFortRarity.Uncommon);
                var image = CreateDisplayImage(iconBitmap, rarity);
                previewBitmap = image.ToWriteableBitmap();
            }
            

            StyleDatas.Add(new ObjectStyleData(style.Name, style, previewBitmap));
        }

        SelectedStyleIndex = 0;
    }
    
    public SKBitmap CreateDisplayImage(SKBitmap iconBitmap, EFortRarity rarity = EFortRarity.Uncommon)
    {
        var bitmap = new SKBitmap(64, 64, iconBitmap.ColorType, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bitmap))
        {
            var colors = UEParse.RarityColors[(int) rarity];
            var backgroundRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
            var backgroundPaint = new SKPaint { Shader = SkiaExtensions.RadialGradient(bitmap.Height, colors.Color1, colors.Color3) };
            canvas.DrawRect(backgroundRect, backgroundPaint);
            canvas.DrawBitmap(iconBitmap, backgroundRect);
        }

        return bitmap;
    }
}