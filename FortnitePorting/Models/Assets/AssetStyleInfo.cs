using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using DynamicData;
using FortnitePorting.Extensions;
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
    
    
    public AssetStyleInfo(string channelName, FStructFallback[] styles, Bitmap fallbackPreviewImage, bool addDefault = false)
    {
        ChannelName = channelName;
        if (addDefault)
        {
            var noneIcon = UEParse.Provider.LoadPackageObject<UTexture2D>("/Game/UI/Foundation/Textures/Icons/Locker/T_Ui_Elastic_NoColor");
            StyleDatas.Add(new AssetStyleData("Universal", new FStructFallback(), noneIcon.Decode()!.ToWriteableBitmap()));
        }
        
        foreach (var style in styles)
        {
            if (style.GetOrDefault<FText?>("VariantName") is not { } variantNameText 
                || variantNameText.Text.Equals("Empty", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // TODO: If addDefault and variantNameText = "", use RequiredCondition value as name?
            
            var previewBitmap = fallbackPreviewImage;
            if (style.TryGetValue(out UTexture2D previewTexture, "PreviewImage"))
            {
                previewBitmap = previewTexture.Decode()!.ToWriteableBitmap();
            }
            // TODO: Add color-based icons for color styles?

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
    
    public AssetStyleInfo(string channelName, UObject colorVariant, bool isParamSet = false)
    {
        ChannelName = channelName;

        StyleDatas.AddRange(isParamSet ? ParseParamSetStyles(colorVariant) : ParseColorSwatchStyles(colorVariant));

        SelectedStyleIndex = 0;
    }
    
    private List<AssetColorStyleData> ParseColorSwatchStyles(UObject colorVariant)
    {
        List<AssetColorStyleData> colorStyles = [];
        if (!colorVariant.TryGetValue(out FStructFallback inlineVar, "InlineVariant")
            || !inlineVar.TryGetValue(out FStructFallback richColorVar, "RichColorVar")
            || !richColorVar.TryGetValue(out FSoftObjectPath swatchPath, "ColorSwatchForChoices")
            || !swatchPath.TryLoad(out UObject colorSwatch)
            || !colorSwatch.TryGetValue(out FStructFallback[] colorPairs, "ColorPairs"))
            return colorStyles;
        
        foreach (var color in colorPairs)
        {
            var colorValue = color.GetOrDefault<FLinearColor>("ColorValue");
            var colorName = color.GetOrDefault("ColorName", new FName(colorValue.Hex));
            var displayIcon = CreateColorDisplayImage(colorValue);
            
            colorStyles.Add(new AssetColorStyleData(colorName.PlainText, richColorVar, color, displayIcon.ToWriteableBitmap()));
        }

        return colorStyles;
    }
    
    private List<AssetColorStyleData> ParseParamSetStyles(UObject colorVariant)
    {
        List<AssetColorStyleData> colorStyles = [];
        if (!colorVariant.TryGetValue(out FStructFallback inlineVar, "InlineVariant")
            || !inlineVar.TryGetValue(out UObject paramSet, "MaterialParameterSetChoices")
            || !paramSet.TryGetValue(out FStructFallback[] choices, "Choices"))
            return colorStyles;
        
        foreach (var color in choices)
        {
            if (!color.TryGetValue(out FInstancedStruct uiStruct, "UITileDisplayData")
                || !uiStruct.NonConstStruct.TryGetValue(out FLinearColor colorValue, "Color"))
                continue;
            
            var colorName = color.GetOrDefault("DisplayName", new FText(colorValue.Hex));
            var displayIcon = CreateColorDisplayImage(colorValue);
            
            colorStyles.Add(new AssetColorStyleData(colorName.Text, inlineVar, color, displayIcon.ToWriteableBitmap(), true));
        }

        return colorStyles;
    }
    
    public AssetStyleInfo(string channelName, IEnumerable<BaseStyleData> styles, bool multiSelect = false)
    {
        MultiSelect = multiSelect;
        ChannelName = channelName;
        
        foreach (var style in styles)
        {
            StyleDatas.Add(style);
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
    
    public SKBitmap CreateColorDisplayImage(FLinearColor color)
    {
        var bitmap = new SKBitmap(64, 64, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bitmap))
        {
            var backgroundRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
            var backgroundPaint = new SKPaint { Shader = SKShader.CreateColor(SKColor.Parse(color.Hex)) };
            canvas.DrawRect(backgroundRect, backgroundPaint);
        }

        return bitmap;
    }
}