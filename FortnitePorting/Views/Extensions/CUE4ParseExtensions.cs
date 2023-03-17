using System;
using System.Linq;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;

namespace FortnitePorting.Views.Extensions;

public static class CUE4ParseExtensions
{
    public static T GetOrDefault<T>(this UObject obj, params string[] names)
    {
        foreach (var name in names)
        {
            if (obj.Properties.Any(x => x.Name.Text.Equals(name)))
            {
                return obj.GetOrDefault<T>(name);
            }
        }

        return default;
    }

    public static FName? GetValueOrDefault(this FGameplayTagContainer tags, string category, FName def = default)
    {
        return tags.GameplayTags is not { Length: > 0 } ? def : tags.GameplayTags.FirstOrDefault(it => it.Text.StartsWith(category), def);
    }

    public static bool ContainsAny(this FGameplayTagContainer tags, params string[] check)
    {
        return check.Any(x => tags.ContainsAny(x));
    }

    public static bool ContainsAny(this FGameplayTagContainer tags, string check)
    {
        if (tags.GameplayTags is null) return false;
        return tags.GameplayTags.Any(x => x.Text.Contains(check));
    }

    public static Image<T>? DecodeImageSharp<T>(this UTexture2D texture) where T : unmanaged, IPixel<T>
    {
        return (Image<T>?)DecodeImageSharp(texture);
    }

    public static Image? DecodeImageSharp(this UTexture2D texture)
    {
        var mip = texture.GetFirstMip();
        if (mip is null) return null;

        TextureDecoder.DecodeTexture(mip, texture.Format, texture.isNormalMap, ETexturePlatform.DesktopMobile, out var data, out var colorType);
        Image image;
        switch (colorType)
        {
            case SKColorType.Alpha8:
                image = Image.LoadPixelData<A8>(data, mip.SizeX, mip.SizeY);
                break;
            case SKColorType.Rgba8888:
                image = Image.LoadPixelData<Rgba32>(data, mip.SizeX, mip.SizeY);
                break;
            case SKColorType.Rgb888x:
                var fixedImage = new Image<Rgb24>(mip.SizeX, mip.SizeY);
                Image.LoadPixelData<Rgba32>(data, mip.SizeX, mip.SizeY).IteratePixels((color, x, y) => { fixedImage[x, y] = color.Rgb; });
                image = fixedImage;
                break;
            case SKColorType.Bgra8888:
                image = Image.LoadPixelData<Bgra32>(data, mip.SizeX, mip.SizeY);
                break;
            case SKColorType.Rgba1010102:
                image = Image.LoadPixelData<Rgba1010102>(data, mip.SizeX, mip.SizeY);
                break;
            case SKColorType.Gray8:
                image = Image.LoadPixelData<L8>(data, mip.SizeX, mip.SizeY);
                break;
            case SKColorType.Rgba16161616:
                image = Image.LoadPixelData<Rgba64>(data, mip.SizeX, mip.SizeY);
                break;
            default:
                throw new ArgumentException($"Invalid Pixel Type: {colorType}");
        }

        return image;
    }

    private static void IteratePixels<T>(this Image<T> image, Action<T, int, int> action) where T : unmanaged, IPixel<T>
    {
        for (var x = 0; x < image.Width; x++)
        {
            for (var y = 0; y < image.Height; y++)
            {
                action(image[x, y], x, y);
            }
        }
    }
}