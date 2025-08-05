using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CUE4Parse_Conversion.Textures;
using SkiaSharp;

namespace FortnitePorting.Extensions;

public static class ImageExtensions
{
    public static WriteableBitmap ToWriteableBitmap(this CTexture texture, bool ignoreAlpha = false)
    {
        return texture.ToSkBitmap().ToWriteableBitmap(ignoreAlpha);
    }
    
    public static WriteableBitmap ToWriteableBitmap(this SKBitmap skiaBitmap, bool ignoreAlpha = false)
    {
        using var skiaPixmap = skiaBitmap.PeekPixels();
        using var skiaImage = SKImage.FromPixels(skiaPixmap);

        var bitmapColorType = skiaBitmap.ColorType switch
        {
            SKColorType.Rgba8888 => PixelFormat.Rgba8888,
            SKColorType.Bgra8888 => PixelFormat.Bgra8888,
            SKColorType.Rgb565 => PixelFormat.Rgb565,
            SKColorType.RgbaF32 => PixelFormat.Rgb32,
            SKColorType.Gray8 => PixelFormats.Gray8,
        };
        
        var bitmap = new WriteableBitmap(new PixelSize(skiaBitmap.Width, skiaBitmap.Height), new Vector(96, 96), bitmapColorType, ignoreAlpha ? AlphaFormat.Opaque : AlphaFormat.Unpremul);
        var frameBuffer = bitmap.Lock();

        using (var pixmap = new SKPixmap(new SKImageInfo(skiaBitmap.Width, skiaBitmap.Height, skiaBitmap.ColorType, ignoreAlpha ? SKAlphaType.Opaque : SKAlphaType.Unpremul), frameBuffer.Address, frameBuffer.RowBytes))
        {
            skiaImage.ReadPixels(pixmap, 0, 0);
        }
        
        frameBuffer.Dispose();
        return bitmap;

    }
    
    public static ConcurrentDictionary<string, Bitmap> CachedBitmaps = [];

    public static Bitmap AvaresBitmap(string path)
    {
        if (CachedBitmaps.TryGetValue(path, out var existingBitmap))
        {
            return existingBitmap;
        }

        var uri = new Uri(path);
        var stream = AssetLoader.Open(uri);
        var bitmap = new Bitmap(stream);
        CachedBitmaps[path] = bitmap;
        return bitmap;

    }
    
    public static Color LerpColor(Color color1, Color color2, double factor)
    {
        return new Color(
            (byte)(color1.A + (color2.A - color1.A) * factor),
            (byte)(color1.R + (color2.R - color1.R) * factor),
            (byte)(color1.G + (color2.G - color1.G) * factor),
            (byte)(color1.B + (color2.B - color1.B) * factor));
    }
    
    public static Color LerpColor(Color color1, double alpha1, Color color2, double alpha2, double factor)
    {
        return new Color(
            (byte)((alpha1 + (alpha2 - alpha1) * factor) * byte.MaxValue),
            (byte)(color1.R + (color2.R - color1.R) * factor),
            (byte)(color1.G + (color2.G - color1.G) * factor),
            (byte)(color1.B + (color2.B - color1.B) * factor));
    }
    
    public static Bitmap GetMedalBitmap(int ranking = -1)
    {
        return AvaresBitmap($"avares://FortnitePorting/Assets/FN/{ranking switch {
            1 => "GoldMedal",
            2 => "SilverMedal",
            3 => "BronzeMedal",
            _ => "NormalMedal"
        }}.png");
    }
}
