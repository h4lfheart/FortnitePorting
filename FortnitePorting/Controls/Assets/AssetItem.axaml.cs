using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.ViewModels;
using SkiaSharp;
using SkiaExtensions = FortnitePorting.Extensions.SkiaExtensions;

namespace FortnitePorting.Controls.Assets;

public partial class AssetItem : UserControl
{
    public static readonly StyledProperty<bool> IsFavoriteProperty = AvaloniaProperty.Register<AssetItem, bool>(nameof(IsFavoriteProperty));

    public bool IsFavorite
    {
        get => GetValue(IsFavoriteProperty);
        set => SetValue(IsFavoriteProperty, value);
    }

    public bool Hidden { get; set; }
    public EAssetType Type { get; set; }
    public UObject Asset { get; set; }
    public string ID { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }

    public FGameplayTagContainer? GameplayTags { get; set; }
    public EFortRarity Rarity { get; set; }
    public int Season { get; set; }
    public string Series { get; set; }

    public Bitmap IconBitmap { get; set; }
    public Bitmap PreviewImage { get; set; }
    
    public float DisplayWidth { get; set; }
    public float DisplayHeight { get; set; }
    

    public AssetItem(UObject asset, UTexture2D icon, string displayName, EAssetType type, bool isHidden = false, bool hideRarity = false, EFortRarity? rarityOverride = null, bool useTitleCase = true)
    {
        DataContext = this;
        InitializeComponent();

        DisplayWidth = 64 * AppSettings.Current.AssetSizeMultiplier;
        DisplayHeight = 80 * AppSettings.Current.AssetSizeMultiplier;

        Hidden = isHidden;
        Type = type;
        Asset = asset;
        IsFavorite = AppSettings.Current.FavoritePaths.Contains(asset.GetPathName());
        ID = asset.Name;
        DisplayName = useTitleCase ? displayName.TitleCase() : displayName;
        Description = asset.GetOrDefault("Description", new FText("No description.")).Text;
        Rarity = rarityOverride ?? asset.GetOrDefault("Rarity", EFortRarity.Uncommon);
        GameplayTags = asset.GetOrDefault<FGameplayTagContainer?>("GameplayTags");

        var seasonTag = GameplayTags?.GetValueOrDefault("Cosmetics.Filter.Season.")?.Text;
        Season = int.TryParse(seasonTag?.SubstringAfterLast("."), out var seasonNumber) ? seasonNumber : int.MaxValue;

        var series = Asset.GetOrDefault<UObject?>("Series");
        Series = series?.GetOrDefault<FText>("DisplayName").Text ?? string.Empty;

        var iconBitmap = icon.Decode()!;
        IconBitmap = new Bitmap(iconBitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream());

        var fullBitmap = new SKBitmap(128, 160, iconBitmap.ColorType, iconBitmap.AlphaType);
        using (var fullCanvas = new SKCanvas(fullBitmap))
        {
            var seriesBackground = series?.GetOrDefault<UTexture2D?>("BackgroundTexture");
            var seriesColors = series?.GetOrDefault<RarityCollection?>("Colors");
            if (seriesBackground is not null)
                fullCanvas.DrawBitmap(seriesBackground.Decode(), new SKRect(0, 0, fullBitmap.Width, fullBitmap.Height));
            else if (seriesColors is not null)
                fullCanvas.DrawRect(new SKRect(0, 0, fullBitmap.Width, fullBitmap.Height), new SKPaint
                {
                    Shader = SkiaExtensions.RadialGradient(fullBitmap.Height, seriesColors.Color1, seriesColors.Color3)
                });
            else
                fullCanvas.DrawRect(new SKRect(0, 0, fullBitmap.Width, fullBitmap.Height), new SKPaint
                {
                    Shader = SkiaExtensions.RadialGradient(fullBitmap.Height, SKColor.Parse("#50C8FF"), SKColor.Parse("#1B7BCF"))
                });

            fullCanvas.DrawBitmap(iconBitmap, new SKRect(-16, 0, fullBitmap.Width + 16, fullBitmap.Height));
            if (!hideRarity)
            {
                var colors = seriesColors ?? CUE4ParseVM.RarityColors[(int) Rarity];
                fullCanvas.RotateDegrees(-4);
                fullCanvas.DrawRect(new SKRect(-16, fullBitmap.Height - 12, fullBitmap.Width + 16, fullBitmap.Height + 16), new SKPaint
                {
                    Color = SKColor.Parse(colors.Color1.Hex).WithAlpha(204) // 0.8 Alpha
                });
                fullCanvas.RotateDegrees(4);
            }
        }

        PreviewImage = new Bitmap(fullBitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream());
    }

    public bool Match(string filter)
    {
        return MiscExtensions.Filter(DisplayName, filter) || MiscExtensions.Filter(ID, filter);
    }

    public void ChangeSize(float multiplier)
    {
        
    }

    public void Favorite()
    {
        var path = Asset.GetPathName();
        if (!AppSettings.Current.FavoritePaths.Contains(path))
        {
            AppSettings.Current.FavoritePaths.AddUnique(path);
            IsFavorite = true;
        }
        else
        {
            AppSettings.Current.FavoritePaths.Remove(path);
            IsFavorite = false;
        }
    }

    public void CopyPath()
    {
        Clipboard.SetTextAsync(Asset.GetPathName());
    }

    public void CopyID()
    {
        Clipboard.SetTextAsync(Asset.Name);
    }
}