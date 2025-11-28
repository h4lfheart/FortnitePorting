using System;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace FortnitePorting.Extensions;

public delegate void PixelReference<T>(ref T pixel, int index) where T : IPixel;

public static class ImageSharpExtensions
{
    extension(UTexture texture)
    {
        public Image<Rgba32>? DecodeImageSharp()
        {
            if (texture is UVirtualTexture2D)
            {
                throw new NotSupportedException("Virtual textures cannot be exported as .tga at this time.");
            }
        
            var mip = texture.GetFirstMip();
            if (mip is null) return null;
        
            var bitmap = texture.Decode(mip)?.ToSkBitmap();
            if (bitmap is null) return null;
        
            Image returnImage = bitmap.ColorType switch
            {
                SKColorType.Rgba8888 => Image.LoadPixelData<Rgba32>(bitmap.GetPixelSpan(), mip.SizeX, mip.SizeY),
                SKColorType.Bgra8888 => Image.LoadPixelData<Bgra32>(bitmap.GetPixelSpan(), mip.SizeX, mip.SizeY),
                SKColorType.Rgb888x => Image.LoadPixelData<Rgb24>(bitmap.GetPixelSpan(), mip.SizeX, mip.SizeY)
            };

            return returnImage.CloneAs<Rgba32>();
        }
    }
}