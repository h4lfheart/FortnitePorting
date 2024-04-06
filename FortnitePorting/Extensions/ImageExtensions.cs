using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace FortnitePorting.Extensions;

public static class ImageExtensions
{
    public static Image<Rgba32>? DecodeImageSharp(this UTexture texture)
    {
        var mip = texture.GetFirstMip();
        if (mip is null) return null;

        TextureDecoder.DecodeTexture(mip, texture.Format, texture.IsNormalMap, ETexturePlatform.DesktopMobile, out var data, out var colorType);
        
        return colorType switch
        {
            SKColorType.Rgba8888 => Image.LoadPixelData<Rgba32>(data, mip.SizeX, mip.SizeY),
            SKColorType.Bgra8888 => Image.LoadPixelData<Bgra32>(data, mip.SizeX, mip.SizeY).CloneAs<Rgba32>(),
            _ => throw new ArgumentException($"Invalid Pixel Type: {colorType}")
        };
    }
    
    public static Bitmap ToBitmap(this Image texture)
    {
        var stream = new MemoryStream();
        texture.SaveAsPng(stream);
        stream.Position = 0;
        
        return new Bitmap(stream);
    }
    
    public static void IteratePixels<T>(this Image<T> image, Action<T, int, int> action) where T : unmanaged, IPixel<T>
    {
        for (var x = 0; x < image.Width; x++)
        {
            for (var y = 0; y < image.Height; y++)
            {
                action(image[x, y], x, y);
            }
        }
    }
    
    public static void IteratePixelsDual(this Image<Rgba32> src, Image<Rgba32> dst, DualPixelRef action)
    {
        var operation = new ActionRowOperation(src.Frames[0].PixelBuffer, dst.Frames[0].PixelBuffer, action);
        ParallelRowIterator.IterateRows(Configuration.Default, Rectangle.Intersect(src.Bounds, dst.Bounds), in operation);
    }
    
    public delegate void DualPixelRef(ref Rgba32 srcPixel, ref Rgba32 dstPixel);
    private readonly struct ActionRowOperation : IRowOperation<Rgba32>, IRowOperation
    {
        private readonly Buffer2D<Rgba32> Source;
        private readonly Buffer2D<Rgba32> Destination;
        private readonly DualPixelRef Action;

        public ActionRowOperation(Buffer2D<Rgba32> source, Buffer2D<Rgba32> destination, DualPixelRef action)
        {
            Source = source;
            Destination = destination;
            Action = action;
        }

        public int GetRequiredBufferLength(Rectangle bounds)
        {
            return 0;
        }

        public void Invoke(int y, Span<Rgba32> srcSpan)
        {
            var dstSpan = Destination.DangerousGetRowSpan(y);
            for (var i = 0; i < dstSpan.Length; i++)
            {
                Action(ref srcSpan[i], ref dstSpan[i]);
            }
        }

        public void Invoke(int y)
        {
            
        }
    }
}