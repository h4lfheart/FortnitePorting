using System.Windows.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using SkiaSharp;

namespace FortnitePorting.Views.Extensions;

public static class ImageExtensions
{
    public static BitmapSource ToBitmapSource(this SKBitmap bitmap)
    {
        var source = new BitmapImage { CacheOption = BitmapCacheOption.OnDemand};
        source.BeginInit();
        source.StreamSource = bitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
        source.EndInit();
        return source;
    }
}