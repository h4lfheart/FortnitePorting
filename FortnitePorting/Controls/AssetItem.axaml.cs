using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.ViewModels;
using SharpGLTF.Schema2;
using SkiaSharp;
using SkiaExtensions = FortnitePorting.Extensions.SkiaExtensions;

namespace FortnitePorting.Controls;

public partial class AssetItem : UserControl
{
    public UObject Asset { get; set; }
    public bool IsRandom { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public EFortRarity Rarity { get; set; }
    public Bitmap IconBitmap { get; set; }
    public Bitmap PreviewImage { get; set; }
    
    public AssetItem(UObject asset, UTexture2D icon, bool isRandom = false)
    {
        InitializeComponent();

        Asset = asset;
        IsRandom = isRandom;
        DisplayName = asset.GetOrDefault("DisplayName", new FText("Unnammed")).Text;
        Description = asset.GetOrDefault("Description", new FText("No description.")).Text;
        Rarity = asset.GetOrDefault("Rarity", EFortRarity.Uncommon);
        
        var iconBitmap = icon.Decode()!;
        IconBitmap = new Bitmap(iconBitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream());
        
        var fullBitmap = new SKBitmap(128, 160, iconBitmap.ColorType, iconBitmap.AlphaType);
        using (var fullCanvas = new SKCanvas(fullBitmap))
        {
            var series = Asset.GetOrDefault<UObject?>("Series");
            var seriesBackground = series?.GetOrDefault<UTexture2D?>("BackgroundTexture");
            var seriesColors = series?.GetOrDefault<RarityCollection?>("Colors");
            if (seriesBackground is not null)
            {
                fullCanvas.DrawBitmap(seriesBackground.Decode(), new SKRect(0, 0, fullBitmap.Width, fullBitmap.Height));
            }
            else if (seriesColors is not null)
            {
                fullCanvas.DrawRect(new SKRect(0, 0, fullBitmap.Width, fullBitmap.Height), new SKPaint
                {
                    Shader = SkiaExtensions.RadialGradient(fullBitmap.Height, seriesColors.Color1, seriesColors.Color3)
                });
            }
            else if (IsRandom)
            {
                var commonColors = CUE4ParseVM.RarityColors[0];
                fullCanvas.DrawRect(new SKRect(0, 0, fullBitmap.Width, fullBitmap.Height), new SKPaint
                {
                    Shader = SkiaExtensions.RadialGradient(fullBitmap.Height, commonColors.Color1, commonColors.Color3)
                });
            }
            else
            {
                fullCanvas.DrawRect(new SKRect(0, 0, fullBitmap.Width, fullBitmap.Height), new SKPaint
                {
                    Shader = SkiaExtensions.RadialGradient(fullBitmap.Height, SKColor.Parse("#50C8FF"), SKColor.Parse("#1B7BCF"))
                });
            }
            
            
            var colors = seriesColors ?? CUE4ParseVM.RarityColors[(int) Rarity];
            fullCanvas.DrawBitmap(iconBitmap, new SKRect(-16, 0, fullBitmap.Width+16, fullBitmap.Height));
            fullCanvas.RotateDegrees(-4);
            fullCanvas.DrawRect(new SKRect(-16, fullBitmap.Height-12, fullBitmap.Width+16, fullBitmap.Height+16), new SKPaint
            {
                Color = SKColor.Parse(colors.Color1.Hex).WithAlpha(204) // 0.8 Alpha
            });
            fullCanvas.RotateDegrees(4);
        }
        
        PreviewImage = new Bitmap(fullBitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream());
    }
}