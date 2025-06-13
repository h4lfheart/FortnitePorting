using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Clipboard;
using FortnitePorting.Models.Fortnite;


using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using SkiaSharp;
using SkiaExtensions = FortnitePorting.Extensions.SkiaExtensions;

namespace FortnitePorting.Models.Assets.Asset;


public partial class AssetItem : Base.BaseAssetItem
{
    public new AssetItemCreationArgs CreationData
    {
        get => (AssetItemCreationArgs) base.CreationData;
        private init => base.CreationData = value;
    }

    public EFortRarity Rarity { get; set; }
    public int Season { get; set; }
    public UFortItemSeriesDefinition? Series { get; set; }
    

    private static SKColor InnerBackgroundColor = SKColor.Parse("#50C8FF");
    private static SKColor OuterBackgroundColor = SKColor.Parse("#1B7BCF");

    private static ConcurrentDictionary<string, UFortItemSeriesDefinition> SeriesCache = [];
    
    public AssetItem(AssetItemCreationArgs args)
    {
        Id = Guid.NewGuid();
        CreationData = args;

        IsFavorite = AppSettings.Application.FavoriteAssets.Contains(CreationData.Object.GetPathName());

        Rarity = CreationData.Object.GetOrDefault("Rarity", EFortRarity.Uncommon);
        
        var seasonTag = CreationData.GameplayTags?.GetValueOrDefault("Cosmetics.Filter.Season.")?.Text;
        Season = int.TryParse(seasonTag?.SubstringAfterLast("."), out var seasonNumber) ? seasonNumber : int.MaxValue;

        if (CreationData.Object.GetDataListItem<FPackageIndex>("Series") is { } seriesPackage)
        {
            Series = SeriesCache!.GetOrAdd(seriesPackage.Name,
                () => seriesPackage.Load<UFortItemSeriesDefinition>());
        }
        
        var iconBitmap = CreationData.Icon.Decode()!.ToSkBitmap();
        IconDisplayImage = iconBitmap.ToWriteableBitmap();
        DisplayImage = CreateDisplayImage(iconBitmap).ToWriteableBitmap();
    }

    protected sealed override SKBitmap CreateDisplayImage(SKBitmap iconBitmap)
    {
        var bitmap = new SKBitmap(128, 160, iconBitmap.ColorType, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bitmap))
        {
            var colors = Series?.Colors ?? UEParse.RarityColors[(int) Rarity];
            // background
            var backgroundRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
            if (Series?.BackgroundTexture.LoadOrDefault<UTexture2D>() is { } seriesBackground)
            {
                canvas.DrawBitmap(seriesBackground.Decode()?.ToSkBitmap(), backgroundRect);
            }
            else if (!CreationData.HideRarity)
            {
                var backgroundPaint = new SKPaint { Shader = SkiaExtensions.RadialGradient(bitmap.Height, colors.Color1, colors.Color3) };
                canvas.DrawRect(backgroundRect, backgroundPaint);
            }
            else
            {
                var backgroundPaint = new SKPaint { Shader = SkiaExtensions.RadialGradient(bitmap.Height, InnerBackgroundColor, OuterBackgroundColor) };
                canvas.DrawRect(backgroundRect, backgroundPaint);
            }

            canvas.DrawBitmap(iconBitmap, backgroundRect with { Left = -16, Right = bitmap.Width + 16});
            
            if (!CreationData.HideRarity)
            {
                var coolRectPaint = new SKPaint { Shader = SkiaExtensions.LinearGradient(bitmap.Width, true, colors.Color1, colors.Color2) };
                coolRectPaint.Color = coolRectPaint.Color.WithAlpha((byte) (0.75 * byte.MaxValue));

                canvas.RotateDegrees(-4);
                canvas.DrawRect(new SKRect(-16, bitmap.Height - 12, bitmap.Width + 16, bitmap.Height + 16), coolRectPaint);
                canvas.RotateDegrees(4);
            }
            
        }

        return bitmap;
    }
    
    public override async Task CopyPath()
    {
        await App.Clipboard.SetTextAsync(CreationData.Object.GetPathName());
    }

    public override async Task PreviewProperties()
    {
        var assets = await UEParse.Provider.LoadAllObjectsAsync(Exporter.FixPath(CreationData.Object.GetPathName()));
        var json = JsonConvert.SerializeObject(assets, Formatting.Indented);
        PropertiesPreviewWindow.Preview(CreationData.Object.Name, json);
    }
    
    public override async Task CopyIcon(bool withBackground = false)
    {
        await AvaloniaClipboard.SetImageAsync(withBackground ? DisplayImage : IconDisplayImage);
    }
    
    public override void Favorite()
    {
        var path = CreationData.Object.GetPathName();
        if (AppSettings.Application.FavoriteAssets.Add(path))
        {
            IsFavorite = true;
        }
        else
        {
            AppSettings.Application.FavoriteAssets.Remove(path);
            IsFavorite = false;
        }
    }
}