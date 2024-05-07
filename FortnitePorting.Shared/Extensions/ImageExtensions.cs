using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CUE4Parse.UE4.Objects.Core.Misc;
using SkiaSharp;

namespace FortnitePorting.Shared.Extensions;

public static class ImageExtensions
{
    public static unsafe WriteableBitmap ToWriteableBitmap(this SKBitmap skiaBitmap)
    {
        fixed (byte* p = skiaBitmap.Bytes)
        {
            return new WriteableBitmap(PixelFormat.Rgba8888, AlphaFormat.Unpremul, (IntPtr) p, new PixelSize(skiaBitmap.Width, skiaBitmap.Width), new Vector(96, 96), skiaBitmap.RowBytes);
        }
       
    }
}