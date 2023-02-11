using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.Utils;
using FortnitePorting.AppUtils;
using FortnitePorting.Bundles;
using FortnitePorting.Exports;
using FortnitePorting.Exports.Types;
using FortnitePorting.ViewModels;
using FortnitePorting.Views.Extensions;
using SkiaSharp;

namespace FortnitePorting.Views.Controls;

public partial class AssetSelectorItem : INotifyPropertyChanged
{
    public UObject Asset;
    public SKBitmap IconBitmap;
    public SKBitmap FullBitmap;
    public BitmapImage FullSource;
    public FGameplayTagContainer GameplayTags;
    public EAssetType Type;

    public bool IsRandom { get; set; }
    public string DisplayName { get; set; }
    public string DisplayNameSource { get; set; }
    public string Description { get; set; }
    public string TooltipName { get; set; }
    public string ID { get; set; }
    public EFortRarity Rarity { get; set; }
    public int SeasonNumber { get; set; }
    public string Series { get; set; }
    public Visibility FavoriteVisibility { get; set; }

    public float Size { get; set; } = AppSettings.Current.AssetSize * 64;
    public float FavoriteSize  => Size / 4;

    public bool HiddenAsset;

    public AssetSelectorItem(UObject asset, UTexture2D previewTexture, EAssetType type, bool isRandomSelector = false,
        FText? displayNameOverride = null, bool useIdAsDescription = false, bool hiddenAsset = false)
    {
        InitializeComponent();
        DataContext = this;
        Type = type;
        AddFavoriteCommand = new RelayCommand(AddFavorite);
        ExportHDCommand = new RelayCommand(ExportHD);
        ExportAssetsCommand = new RelayCommand(ExportAssets);
        ClipboardCommand = new RelayCommand<string>(CopyIconToClipboard);

        if (AppSettings.Current.LightMode) 
        {
            FavoriteImage.Effect = new InvertEffect();
            TexturesImage.Effect = new InvertEffect();
            ClipboardImage.Effect = new InvertEffect();
            ExportImage.Effect = new InvertEffect();
        }

        Asset = asset;
        var displayName = displayNameOverride;
        displayName ??= asset.GetOrDefault("DisplayName", new FText("Unnamed"));
        HiddenAsset = hiddenAsset;

        DisplayName = displayName.Text;
        if (DisplayName.Equals("TBD"))
        {
            DisplayName = asset.Name;
        }
        if (displayName.TextHistory is FTextHistory.Base textHistory)
            DisplayNameSource = textHistory.SourceString;
        ID = asset.Name;
        Description = useIdAsDescription ? ID : asset.GetOrDefault("Description", new FText("No description.")).Text;

        Rarity = asset.GetOrDefault("Rarity", EFortRarity.Uncommon);
        GameplayTags = asset.GetOrDefault<FGameplayTagContainer>("GameplayTags");

        var seasonTag = GameplayTags.GetValueOrDefault("Cosmetics.Filter.Season.")?.Text.SubstringAfterLast(".");
        SeasonNumber = int.TryParse(seasonTag, out var seasonNumber) ? seasonNumber : int.MaxValue;
        if (asset.TryGetValue<UObject>(out var series, "Series"))
        {
            Series = series.GetOrDefault<FText>("DisplayName").Text;
        }

        TooltipName = $"{DisplayName} ({ID})";
        IsRandom = isRandomSelector;
        FavoriteVisibility = AppSettings.Current.FavoriteIDs.Contains(ID) ? Visibility.Visible : Visibility.Collapsed;

        var iconBitmap = previewTexture.Decode();
        if (iconBitmap is null) return;
        IconBitmap = iconBitmap;

        FullBitmap = new SKBitmap(iconBitmap.Width, iconBitmap.Height, iconBitmap.ColorType, iconBitmap.AlphaType);
        using (var fullCanvas = new SKCanvas(FullBitmap))
        {
            DrawBackground(fullCanvas, Math.Max(iconBitmap.Width, iconBitmap.Height));
            fullCanvas.DrawBitmap(iconBitmap, 0, 0);
        }

        FullSource = new BitmapImage { CacheOption = BitmapCacheOption.OnDemand };
        FullSource.BeginInit();
        FullSource.StreamSource = FullBitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
        FullSource.EndInit();

        DisplayImage.Source = FullSource;
    }
    
    private const int MARGIN = 2;
    private void DrawBackground(SKCanvas canvas, int size)
    {
        SKShader BorderShader(params FLinearColor[] colors)
        {
            var parsedColors = colors.Select(x => SKColor.Parse(x.Hex)).ToArray();
            return SKShader.CreateLinearGradient(new SKPoint(size / 2f, size), new SKPoint(size, size / 4f), parsedColors,
                SKShaderTileMode.Clamp);
        }
        SKShader BackgroundShader(params FLinearColor[] colors)
        {
            var parsedColors = colors.Select(x => SKColor.Parse(x.Hex)).ToArray();
            return SKShader.CreateRadialGradient(new SKPoint(size / 2f, size / 2f), size / 5 * 4, parsedColors,
                SKShaderTileMode.Clamp);
        }

        if (Type == EAssetType.Prop)
        {
            canvas.DrawRect(new SKRect(0, 0, size, size), new SKPaint
            {
                Color = SKColor.Parse("707370")
            });
            return;
        }

        if (Asset.TryGetValue(out UObject seriesData, "Series"))
        {
            var colors = seriesData.Get<RarityCollection>("Colors");

            canvas.DrawRect(new SKRect(0, 0, size, size), new SKPaint
            {
                Shader = BorderShader(colors.Color2, colors.Color1)
            });

            if (seriesData.TryGetValue(out UTexture2D background, "BackgroundTexture"))
            {
                canvas.DrawBitmap(background.Decode(), new SKRect(MARGIN, MARGIN, size - MARGIN, size - MARGIN));
            }
            else
            {
                canvas.DrawRect(new SKRect(MARGIN, MARGIN, size - MARGIN, size - MARGIN), new SKPaint
                {
                    Shader = BackgroundShader(colors.Color1, colors.Color3)
                });
            }
        }
        else
        {
            var colorData = AppVM.CUE4ParseVM.RarityData[(int) Rarity];

            canvas.DrawRect(new SKRect(0, 0, size, size), new SKPaint
            {
                Shader = BorderShader(colorData.Color2, colorData.Color1)
            });

            canvas.DrawRect(new SKRect(MARGIN, MARGIN, size - MARGIN, size - MARGIN), new SKPaint
            {
                Shader = BackgroundShader(colorData.Color1, colorData.Color3)
            });
        }
    }

    public bool Match(string filter, bool useRegex = false)
    {
        if (DisplayName is null || ID is null || DisplayNameSource is null) return false;

        if (useRegex)
        {
            return Regex.IsMatch(DisplayName, filter) || Regex.IsMatch(ID, filter) || Regex.IsMatch(DisplayNameSource, filter);
        }

        return DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) || ID.Contains(filter, StringComparison.OrdinalIgnoreCase) || DisplayNameSource.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    public void ToggleFavorite()
    {
        if (AppSettings.Current.FavoriteIDs.Contains(ID))
        {
            FavoriteVisibility = Visibility.Collapsed;
            AppSettings.Current.FavoriteIDs.Remove(ID);
        }
        else
        {
            FavoriteVisibility = Visibility.Visible;
            AppSettings.Current.FavoriteIDs.Add(ID);
        }

        OnPropertyChanged(nameof(FavoriteVisibility));
    }
    
    public void SetSize(float value)
    {
        Size = value * 64;

        OnPropertyChanged(nameof(Size));
        OnPropertyChanged(nameof(FavoriteSize));
    }

    public ICommand AddFavoriteCommand { get; private set; }

    public void AddFavorite() 
    {
        ToggleFavorite();
    }

    
    public ICommand ExportHDCommand { get; private set; }

    public void ExportHD()
    {
        Task.Run(async () =>
        {
            var downloadedBundles = (await BundleDownloader.DownloadAsync(Asset.Name)).ToList();
            if (downloadedBundles.Count <= 0)
            {
                Log.Warning("No Bundles Downloaded for {0}", DisplayName);
                return;
            }

            downloadedBundles.ForEach(AppVM.CUE4ParseVM.Provider.RegisterFile);
            await AppVM.CUE4ParseVM.Provider.MountAsync();

            // TODO FIND BETTER WAY TO GET FILES IN BUNDLE PAKS
            var miniProvider = new FortnitePortingFileProvider(isCaseInsensitive: true, versions: CUE4ParseViewModel.Version);
            downloadedBundles.ForEach(miniProvider.RegisterFile);
            await miniProvider.MountAsync();

            foreach (var (name, _) in miniProvider.Files)
            {
                var loadedTexture = await AppVM.CUE4ParseVM.Provider.TryLoadObjectAsync<UTexture2D>(name.SubstringBeforeLast("."));
                if (loadedTexture is null) continue;

                ExportHelpers.Save(loadedTexture);
            }


            Log.Information("Finished Exporting HD Textures for {0}", DisplayName);
        });
    }

    public ICommand ExportAssetsCommand { get; private set; }

    public void ExportAssets()
    {
        Task.Run(async () =>
        {

            var downloadedBundles = (await BundleDownloader.DownloadAsync(Asset.Name)).ToList();
            if (downloadedBundles.Count > 0)
            {
                downloadedBundles.ForEach(AppVM.CUE4ParseVM.Provider.RegisterFile);
                await AppVM.CUE4ParseVM.Provider.MountAsync();
            }

            var allStyles = new List<FStructFallback>();
            var styles = Asset.GetOrDefault("ItemVariants", Array.Empty<UObject>());
            foreach (var style in styles)
            {
                var channel = style.GetOrDefault("VariantChannelName", new FText("Unknown")).Text.ToLower().TitleCase();
                var optionsName = style.ExportType switch
                {
                    "FortCosmeticCharacterPartVariant" => "PartOptions",
                    "FortCosmeticMaterialVariant" => "MaterialOptions",
                    "FortCosmeticParticleVariant" => "ParticleOptions",
                    _ => null
                };

                if (optionsName is null) continue;

                var options = style.Get<FStructFallback[]>(optionsName);
                if (options.Length == 0) continue;

                allStyles.AddRange(options);
            }

            ExportDataBase exportData = Type switch
            {
                EAssetType.Dance => await DanceExportData.Create(Asset),
                _ => await MeshExportData.Create(Asset, Type, allStyles.ToArray())
            };

            Log.Information("Finished Exporting All Assets for {0}", DisplayName);
        });
    }

    public ICommand ClipboardCommand { get; private set; }

    public void CopyIconToClipboard(string? parameter)
    {
        parameter ??= string.Empty;
        ImageExtensions.SetImage(parameter.Equals("WithoutBackground") ? IconBitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray() : FullBitmap.Encode(SKEncodedImageFormat.Png, 100).ToArray());
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}