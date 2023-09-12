using System;
using System.Linq;
using Avalonia.Controls;
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

namespace FortnitePorting.Controls;

public partial class AssetItem : UserControl
{
    public string DisplayName { get; set; }
    public EFortRarity Rarity { get; set; }
    
    public AssetItem(UObject asset, UTexture2D? icon)
    {
        InitializeComponent();

        DisplayName = asset.GetOrDefault("DisplayName", new FText("Unnammed")).Text;
        Rarity = asset.GetOrDefault("Rarity", EFortRarity.Uncommon);
        
        var iconBitmap = icon?.Decode();
        if (iconBitmap is null) return;
        
        var fullBitmap = new SKBitmap(iconBitmap.Width, iconBitmap.Height, iconBitmap.ColorType, iconBitmap.AlphaType);
        using (var fullCanvas = new SKCanvas(fullBitmap))
        {
            DrawBackground(fullCanvas, Math.Max(iconBitmap.Width, iconBitmap.Height), asset);
            fullCanvas.DrawBitmap(iconBitmap, 0, 0);
        }
        
        var bit = new Bitmap(fullBitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream());
        ImageThing.Source = bit;
    }
    
    
    // TODO REDO, OLD FOR TESTING PURPOSES
    
    private const int MARGIN = 4;
    private void DrawBackground(SKCanvas canvas, int size, UObject obj)
    {
        SKShader BorderShader(params FLinearColor[] colors)
        {
            var parsedColors = colors.Select(x => SKColor.Parse(x.Hex)).ToArray();
            return SKShader.CreateLinearGradient(new SKPoint(size / 2f, size), new SKPoint(size, size / 4f), parsedColors, SKShaderTileMode.Clamp);
        }

        SKShader BackgroundShader(params FLinearColor[] colors)
        {
            var parsedColors = colors.Select(x => SKColor.Parse(x.Hex)).ToArray();
            return SKShader.CreateRadialGradient(new SKPoint(size / 2f, size / 2f), size / 5 * 4, parsedColors, SKShaderTileMode.Clamp);
        }

        if (obj.TryGetValue(out UObject seriesData, "Series"))
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
            var colorData = CUE4ParseVM.RarityColors[(int) Rarity];

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

}