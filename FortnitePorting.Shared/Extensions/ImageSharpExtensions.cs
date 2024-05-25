using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace FortnitePorting.Shared.Extensions;

public static class ImageSharpExtensions
{
    public static Image<Rgba32>? DecodeImageSharp(this UTexture2D texture)
    {
        var mip = texture.GetFirstMip();
        if (mip is null) return null;

        TextureDecoder.DecodeTexture(mip, mip.SizeX, mip.SizeY, mip.SizeZ, texture.Format, texture.IsNormalMap, ETexturePlatform.DesktopMobile, out var data, out var colorType);

        Image returnImage = colorType switch
        {
            SKColorType.Rgba8888 => Image.LoadPixelData<Rgba32>(data, mip.SizeX, mip.SizeY),
            SKColorType.Bgra8888 => Image.LoadPixelData<Bgra32>(data, mip.SizeX, mip.SizeY),
        };

        return returnImage.CloneAs<Rgba32>();
    }
    
    public delegate void PixelReference<T>(ref T pixel, int index) where T : IPixel;
    
    public static void PixelOperations<T>(this Image<T> image, PixelReference<T> action) where T : unmanaged, IPixel<T>
    {
        var pixelIndex = 0;
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var pixelRow = accessor.GetRowSpan(y);
                foreach (ref var pixel in pixelRow)
                {
                    action(ref pixel, pixelIndex);
                    pixelIndex++;
                }
            }
        });
    }
}