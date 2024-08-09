using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CUE4Parse.UE4.Objects.Core.Misc;
using SkiaSharp;

namespace FortnitePorting.Shared.Extensions;

public static class ImageExtensions
{
    public static WriteableBitmap ToWriteableBitmap(this SKBitmap skiaBitmap, bool ignoreAlpha = false)
    {
        using var skiaPixmap = skiaBitmap.PeekPixels();
        using var skiaImage = SKImage.FromPixels(skiaPixmap);
        
        var bitmap = new WriteableBitmap(new PixelSize(skiaBitmap.Width, skiaBitmap.Height), new Vector(96, 96), PixelFormat.Bgra8888, ignoreAlpha ? AlphaFormat.Opaque : AlphaFormat.Unpremul);
        var frameBuffer = bitmap.Lock();

        using (var pixmap = new SKPixmap(new SKImageInfo(skiaBitmap.Width, skiaBitmap.Height, SKColorType.Bgra8888, ignoreAlpha ? SKAlphaType.Opaque : SKAlphaType.Unpremul), frameBuffer.Address, frameBuffer.RowBytes))
        {
            skiaImage.ReadPixels(pixmap, 0, 0);
        }
        
        frameBuffer.Dispose();
        return bitmap;

    }

    public static Dictionary<string, Bitmap> CachedBitmaps = [];

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
}