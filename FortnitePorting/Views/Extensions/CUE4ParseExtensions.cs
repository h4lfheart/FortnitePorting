using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using SkiaSharp;

namespace FortnitePorting.Views.Extensions;

public static class CUE4ParseExtensions
{
    public static T GetOrDefault<T>(this UObject obj, params string[] names)
    {
        foreach (var name in names)
        {
            if (obj.Properties.Any(x => x.Name.PlainText.Equals(name)))
            {
                return obj.GetOrDefault<T>(name);
            }
        }

        return default;
    }

    public static BitmapSource ToBitmapSource(this UTexture2D texture)
    {
        return texture.Decode()?.ToBitmapSource();
    }

    public static bool MoveToEnd<T>(this List<T> list, Func<T, bool> predicate)
    {
        var found = list.FirstOrDefault(predicate);
        if (found is null) return false;

        var removed = list.Remove(found);
        if (!removed) return false;
        list.Add(found);

        return true;
    }
}