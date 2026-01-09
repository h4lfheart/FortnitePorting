using System;
using System.Collections.Concurrent;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CUE4Parse_Conversion.Textures;
using SkiaSharp;

namespace FortnitePorting.Extensions;

public static class ImageExtensions
{
    extension(CTexture texture)
    {
        public WriteableBitmap ToWriteableBitmap(bool ignoreAlpha = false)
        {
            return texture.ToSkBitmap().ToWriteableBitmap(ignoreAlpha);
        }
    }

    extension(SKBitmap bitmap)
    {
        public WriteableBitmap ToWriteableBitmap(bool ignoreAlpha = false)
        {
            using var skiaPixmap = bitmap.PeekPixels();
            using var skiaImage = SKImage.FromPixels(skiaPixmap);

            var bitmapColorType = bitmap.ColorType switch
            {
                SKColorType.Rgba8888 => PixelFormat.Rgba8888,
                SKColorType.Bgra8888 => PixelFormat.Bgra8888,
                SKColorType.Rgb565 => PixelFormat.Rgb565,
                SKColorType.RgbaF32 => PixelFormat.Rgb32,
                SKColorType.Gray8 => PixelFormats.Gray8,
            };
            
            var writeableBitmap = new WriteableBitmap(new PixelSize(bitmap.Width, bitmap.Height), new Vector(96, 96), bitmapColorType, ignoreAlpha ? AlphaFormat.Opaque : AlphaFormat.Unpremul);
            var frameBuffer = writeableBitmap.Lock();

            using (var pixmap = new SKPixmap(new SKImageInfo(bitmap.Width, bitmap.Height, bitmap.ColorType, ignoreAlpha ? SKAlphaType.Opaque : SKAlphaType.Unpremul), frameBuffer.Address, frameBuffer.RowBytes))
            {
                skiaImage.ReadPixels(pixmap, 0, 0);
            }
            
            frameBuffer.Dispose();
            return writeableBitmap;

        }
        
        public SKBitmap ToOpacityMask()
        {
            var width = bitmap.Width;
            var height = bitmap.Height;
        
            var maskBitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        
            var sourcePixels = bitmap.GetPixels();
            var maskPixels = maskBitmap.GetPixels();
        
            var pixelCount = width * height;
        
            unsafe
            {
                var srcPtr = (byte*)sourcePixels.ToPointer();
                var dstPtr = (byte*)maskPixels.ToPointer();
            
                for (var i = 0; i < pixelCount; i++)
                {
                    byte luminance;
                    if (bitmap.ColorType == SKColorType.Gray8)
                    {
                        luminance = srcPtr[i];
                    }
                    else
                    {
                        var srcOffset = i * bitmap.BytesPerPixel;
                        luminance = srcPtr[srcOffset];
                    }
                
                    var dstOffset = i * 4;
                    dstPtr[dstOffset + 0] = luminance;
                    dstPtr[dstOffset + 1] = luminance;
                    dstPtr[dstOffset + 2] = luminance;
                    dstPtr[dstOffset + 3] = luminance;
                }
            }

            return maskBitmap;
        }
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
}
