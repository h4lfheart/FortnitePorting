using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Exporting;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Clipboard;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Services;
using FortnitePorting.Views;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using SkiaSharp;
using SkiaExtensions = FortnitePorting.Extensions.SkiaExtensions;

namespace FortnitePorting.Models.Assets.Asset;


public class AssetItem : Base.BaseAssetItem
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

    public const int INVALID_SEASON = int.MaxValue;

    private static SKColor InnerBackgroundColor = SKColor.Parse("#2bb5f3");
    private static SKColor OuterBackgroundColor = SKColor.Parse("#174a89");

    private static ConcurrentDictionary<string, UFortItemSeriesDefinition> SeriesCache = [];
    private static ConcurrentDictionary<string, WriteableBitmap> BackgroundCache = [];

    public static void ResetCaches()
    {
        SeriesCache.Clear();
        
        foreach (var bitmap in BackgroundCache.Values)
            bitmap.Dispose();
        BackgroundCache.Clear();
    }
    
    public AssetItem(AssetItemCreationArgs args)
    {
        Id = Guid.NewGuid();
        CreationData = args;

        IsFavorite = AppSettings.Application.FavoriteAssets.Contains(CreationData.Object.GetPathName());

        Rarity = CreationData.Object.GetOrDefault("Rarity", EFortRarity.Uncommon);
        
        if (CreationData.Object.GetDataListItem<FName?>("Rarity") is { } dataListRarityName
            && Enum.TryParse(dataListRarityName.Text.SubstringAfter("::"), out EFortRarity dataListRarity))
            Rarity = dataListRarity;

        if (CreationData.GameplayTags.GetValueOrDefault("Cosmetics.Set")?.Text is { } setTag &&
            UEParse.SetNames.TryGetValue(setTag, out var setName))
        {
            SetName = setName;
        }
        
        var seasonTag = CreationData.GameplayTags.GetValueOrDefault("Cosmetics.Filter.Season.")?.Text;
        Season = int.TryParse(seasonTag?.SubstringAfterLast("."), out var seasonNumber) ? seasonNumber : INVALID_SEASON;

        if (CreationData.Object.GetDataListItem<FPackageIndex>("Series") is { } seriesPackage)
        {
            Series = SeriesCache.GetOrAdd(seriesPackage.Name,
                _ => seriesPackage.Load<UFortItemSeriesDefinition>());
        }
    }

    public async Task LoadBitmapAsync()
    {
        if (CreationData.IconPath is not { } iconPath) return;

        IconDisplayImage = await TryLoadIconBitmapAsync(iconPath);

        if (IconDisplayImage is null)
        {
            var placeholderPath = AssetLoading.Get(CreationData.ExportType).PlaceholderIconPath;
            if (!string.Equals(iconPath, placeholderPath, StringComparison.OrdinalIgnoreCase))
                IconDisplayImage = await TryLoadIconBitmapAsync(placeholderPath);
        }

        BackgroundImage = CreateBackgroundImage();
    }

    private static async Task<WriteableBitmap?> TryLoadIconBitmapAsync(string iconPath)
    {
        try
        {
            var texture = await UEParse.Provider!.SafeLoadPackageObjectAsync<UTexture2D>(iconPath);
            using var iconBitmap = texture?.Decode()?.ToSkBitmap();
            return iconBitmap?.ToWriteableBitmap();
        }
        catch
        {
            return null;
        }
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
        BackgroundCache.TryAdd(backgroundKey, bitmap);
        return bitmap;
    }

    public override async Task NavigateTo()
    {
        Navigation.App.Open<FilesView>();

        var assetPath = UEParse.Provider!.FixPath(CreationData.Object.GetPathName().SubstringBefore("."));
        FilesVM.JumpTo(assetPath);
        
        AppWM.Window.BringToTop();
    }

    public override async Task CopyPath()
    {
        await App.Clipboard.SetTextAsync(CreationData.Object.GetPathName());
    }

    public override async Task PreviewProperties()
    {
        var assets = await UEParse.Provider!.LoadAllObjectsAsync(Exporter.FixPath(CreationData.Object.GetPathName()));
        var json = JsonConvert.SerializeObject(assets, Formatting.Indented);
        PropertiesPreviewWindow.Preview(CreationData.Object.Name, json);
    }
    
    public override async Task SaveIcon()
    {
        var iconPath = CreationData.HighResIconPath ?? CreationData.LowResIconPath;
        if (iconPath is null || await UEParse.Provider!.SafeLoadPackageObjectAsync<UTexture2D>(iconPath) is not { } texture)
        {
            Info.Message("Save Icon", "Failed to save icon, no valid icon stored.");
            return;
        }
        
        if (await App.SaveFileDialog(suggestedFileName: texture.Name, Globals.PNGFileType) is not { } savePath) 
            return;

        using var bitmap = texture.Decode()?.ToSkBitmap()?.ToWriteableBitmap();
        bitmap?.Save(savePath);
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

    public override async Task SendToUser()
    {
        var path = CreationData.Object.GetPathName();
        var (icon, displayName, _) = await UEParse.ResolveGameFileAsync(path);
        TaskService.RunDispatcher(() =>
        {
            ChatVM.PendingGameFile = new PendingGameFileAttachment(path, icon, displayName);
            Navigation.App.Open<ChatView>();
        });
    }
}