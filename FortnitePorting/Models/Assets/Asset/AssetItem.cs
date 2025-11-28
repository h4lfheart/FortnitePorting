using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Clipboard;
using FortnitePorting.Models.Fortnite;


using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Views;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using Serilog;
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
    public string? SetName { get; set; }
    

    private static SKColor InnerBackgroundColor = SKColor.Parse("#2bb5f3");
    private static SKColor OuterBackgroundColor = SKColor.Parse("#174a89");

    private static ConcurrentDictionary<string, UFortItemSeriesDefinition> SeriesCache = [];
    private static ConcurrentDictionary<string, WriteableBitmap> BackgroundCache = [];
    
    public AssetItem(AssetItemCreationArgs args)
    {
        Id = Guid.NewGuid();
        CreationData = args;

        IsFavorite = AppSettings.Application.FavoriteAssets.Contains(CreationData.Object.GetPathName());

        Rarity = CreationData.Object.GetOrDefault("Rarity", EFortRarity.Uncommon);

        if (CreationData.GameplayTags?.GetValueOrDefault("Cosmetics.Set")?.Text is { } setTag &&
            UEParse.SetNames.TryGetValue(setTag, out var setName))
        {
            SetName = setName;
        }
            
        
        var seasonTag = CreationData.GameplayTags?.GetValueOrDefault("Cosmetics.Filter.Season.")?.Text;
        Season = int.TryParse(seasonTag?.SubstringAfterLast("."), out var seasonNumber) ? seasonNumber : int.MaxValue;

        if (CreationData.Object.GetDataListItem<FPackageIndex>("Series") is { } seriesPackage)
        {
            Series = SeriesCache!.GetOrAdd(seriesPackage.Name,
                () => seriesPackage.Load<UFortItemSeriesDefinition>());
        }
    }

    public void LoadBitmap()
    {
        var iconBitmap = CreationData.Icon.Decode()!.ToSkBitmap();
        IconDisplayImage = iconBitmap.ToWriteableBitmap();
        BackgroundImage = CreateBackgroundImage();
    }

    protected sealed override WriteableBitmap CreateBackgroundImage()
    {
        var backgroundKey = Series?.Name ?? "Default";
        if (BackgroundCache.TryGetValue(backgroundKey, out var existingBackground))
        {
            return existingBackground;
        }
        
        var skiaBitmap = new SKBitmap(128, 160, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(skiaBitmap))
        {
            var backgroundRect = new SKRect(0, 0, skiaBitmap.Width, skiaBitmap.Height);
            if (Series?.Colors is { } colors)
            {
                if (Series?.BackgroundTexture.LoadOrDefault<UTexture2D>() is { } seriesBackground)
                {
                    canvas.DrawBitmap(seriesBackground.Decode()?.ToSkBitmap(), backgroundRect);
                }
                else
                {
                    
                    var backgroundPaint = new SKPaint { Shader = SkiaExtensions.RadialGradient(skiaBitmap.Height, colors.Color1, colors.Color3) };
                    canvas.DrawRect(backgroundRect, backgroundPaint);
                }
            }
            else
            {
                var backgroundPaint = new SKPaint { Shader = SkiaExtensions.RadialGradient(skiaBitmap.Height, InnerBackgroundColor, OuterBackgroundColor) };
                canvas.DrawRect(backgroundRect, backgroundPaint);
            }
            
        }

        var bitmap = skiaBitmap.ToWriteableBitmap();
        BackgroundCache.GetOrAdd(backgroundKey, bitmap);
        return bitmap;
    }

    public override async Task NavigateTo()
    {
        Navigation.App.Open<FilesView>();
        FilesVM.FileViewJumpTo(UEParse.Provider.FixPath(CreationData.Object.GetPathName().SubstringBefore(".")));
        AppWM.Window.BringToTop();
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
        await AvaloniaClipboard.SetImageAsync(IconDisplayImage);
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