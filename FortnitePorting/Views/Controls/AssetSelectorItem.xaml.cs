using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.ViewModels;
using SkiaSharp;

namespace FortnitePorting.Views.Controls;

public partial class AssetSelectorItem
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
}