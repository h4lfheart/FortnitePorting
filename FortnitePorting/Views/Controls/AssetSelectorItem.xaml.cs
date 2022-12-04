using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.AppUtils;
using FortnitePorting.ViewModels;
using SkiaSharp;

namespace FortnitePorting.Views.Controls;

public partial class AssetSelectorItem : INotifyPropertyChanged
{
    public UObject Asset;
    public SKBitmap IconBitmap;
    public SKBitmap FullBitmap;
    public BitmapImage FullSource;
    
    public bool IsRandom { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string TooltipName { get; set; }
    public string ID { get; set; }
    public Visibility FavoriteVisibility { get; set; }

    public AssetSelectorItem(UObject asset, UTexture2D previewTexture, bool isRandomSelector = false)
    {
        InitializeComponent();
        DataContext = this;

        Asset = asset;
        DisplayName = asset.GetOrDefault("DisplayName", new FText("Unnamed")).Text;
        Description = asset.GetOrDefault("Description", new FText("No description.")).Text;
        ID = asset.Name;

        TooltipName = $"{DisplayName} ({ID})";
        IsRandom = isRandomSelector;
        
        var iconBitmap = previewTexture.Decode();
        if (iconBitmap is null) return;
        IconBitmap = iconBitmap;
        
        FullBitmap = new SKBitmap(iconBitmap.Width, iconBitmap.Height, iconBitmap.ColorType, iconBitmap.AlphaType);
        using (var fullCanvas = new SKCanvas(FullBitmap))
        {
            DrawBackground(fullCanvas, Math.Max(iconBitmap.Width, iconBitmap.Height));
            fullCanvas.DrawBitmap(iconBitmap, 0, 0);
        }
        
        FullSource = new BitmapImage { CacheOption = BitmapCacheOption.OnDemand};
        FullSource.BeginInit();
        FullSource.StreamSource = FullBitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
        FullSource.EndInit();

        DisplayImage.Source = FullSource;
        FavoriteVisibility = AppSettings.Current.FavoriteIDs.Contains(ID) ? Visibility.Visible : Visibility.Collapsed;
        //BeginAnimation(OpacityProperty, AppearAnimation);
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
        
        if (Asset.TryGetValue(out UObject seriesData, "Series"))
        {
            var colors = seriesData.Get<RarityCollection>("Colors");
            
            canvas.DrawRect(new SKRect(0, 0, size, size), new SKPaint
            {
                Shader = BorderShader(colors.Color2, colors.Color1)
            });

            if (seriesData.TryGetValue(out UTexture2D background, "BackgroundTexture"))
            {
                canvas.DrawBitmap(background.Decode(), new SKRect(MARGIN, MARGIN, size-MARGIN, size-MARGIN));
            }
            else
            {
                canvas.DrawRect(new SKRect(MARGIN, MARGIN, size-MARGIN, size-MARGIN), new SKPaint
                {
                    Shader = BackgroundShader(colors.Color1, colors.Color3)
                });
            }
        }
        else
        {
            var rarity = Asset.GetOrDefault("Rarity", EFortRarity.Uncommon);
            var colorData = AppVM.CUE4ParseVM.RarityData[(int) rarity];
            
            canvas.DrawRect(new SKRect(0, 0, size, size), new SKPaint
            {
                Shader = BorderShader(colorData.Color2, colorData.Color1)
            });
            
            canvas.DrawRect(new SKRect(MARGIN, MARGIN, size-MARGIN, size-MARGIN), new SKPaint
            {
                Shader = BackgroundShader(colorData.Color1, colorData.Color3)
            });
        }
    }

    public bool Match(string filter, bool useRegex = false)
    {
        if (useRegex)
        {
            return Regex.IsMatch(DisplayName, filter) || Regex.IsMatch(ID, filter);
        }

        return DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) || ID.Contains(filter, StringComparison.OrdinalIgnoreCase);
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