using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.FileProvider;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material.Parameters;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
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
        return tags.GameplayTags is not { Length: > 0 } ? def : tags.GetValue(category);
    }

    public static bool ContainsAny(this FGameplayTagContainer tags, params string[] check)
    {
        return check.Any(x => tags.ContainsAny(x));
    }

    public static bool ContainsAny(this FGameplayTagContainer tags, string check)
    {
        return tags.GameplayTags.Any(x => x.TagName.Text.Contains(check)); 
    }

    public static Image<T>? DecodeImageSharp<T>(this UTexture2D texture) where T : unmanaged, IPixel<T>
    {
        return (Image<T>?) DecodeImageSharp(texture);
    }

    public static Image? DecodeImageSharp(this UTexture2D texture)
    {
        var mip = texture.GetFirstMip();
        if (mip is null) return null;

        var bitmap = texture.Decode(mip);
        if (bitmap is null) return null;
        
        Image image;
        switch (bitmap.ColorType)
        {
            case SKColorType.Alpha8:
                image = Image.LoadPixelData<A8>(bitmap.Bytes, mip.SizeX, mip.SizeY);
                break;
            case SKColorType.Rgba8888:
                image = Image.LoadPixelData<Rgba32>(bitmap.Bytes, mip.SizeX, mip.SizeY);
                break;
            case SKColorType.Rgb888x:
                var fixedImage = new Image<Rgb24>(mip.SizeX, mip.SizeY);
                Image.LoadPixelData<Rgba32>(bitmap.Bytes, mip.SizeX, mip.SizeY).IteratePixels((color, x, y) => { fixedImage[x, y] = color.Rgb; });
                image = fixedImage;
                break;
            case SKColorType.Bgra8888:
                image = Image.LoadPixelData<Bgra32>(bitmap.Bytes, mip.SizeX, mip.SizeY);
                break;
            case SKColorType.Rgba1010102:
                image = Image.LoadPixelData<Rgba1010102>(bitmap.Bytes, mip.SizeX, mip.SizeY);
                break;
            case SKColorType.Gray8:
                image = Image.LoadPixelData<L8>(bitmap.Bytes, mip.SizeX, mip.SizeY);
                break;
            case SKColorType.Rgba16161616:
                image = Image.LoadPixelData<Rgba64>(bitmap.Bytes, mip.SizeX, mip.SizeY);
                break;
            default:
                throw new ArgumentException($"Invalid Pixel Type: {bitmap.ColorType}");
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

    public static bool TryLoadEditorData<T>(this UObject asset, out T? editorData) where T : UObject
    {
        var path = asset.GetPathName().SubstringBeforeLast(".") + ".o.uasset";
        if (AppVM.CUE4ParseVM.Provider.TryLoadObjectExports(path, out var exports))
        {
            editorData = exports.FirstOrDefault() as T;
            return editorData is not null;
        }

        editorData = default;
        return false;
    }

    public static FLinearColor ToLinearColor(this FStaticComponentMaskParameter componentMask)
    {
        return new FLinearColor
        {
            R = componentMask.R ? 1 : 0,
            G = componentMask.G ? 1 : 0,
            B = componentMask.B ? 1 : 0,
            A = componentMask.A ? 1 : 0
        };
    }

    public static bool TryLoadObjectExports(this AbstractFileProvider provider, string path, out IEnumerable<UObject> exports)
    {
        exports = Enumerable.Empty<UObject>();
        try
        {
            exports = provider.LoadAllObjects(path);
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
        catch (AggregateException) // wtf
        {
            return false;
        }

        return true;
    }
}