using System.Diagnostics.CodeAnalysis;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.RenderingX.Data.Textures;
using SkiaSharp;

namespace FortnitePorting.RenderingX.Cache;

public static class TextureCache
{
    private static Dictionary<string, Texture2D> _textures = [];

    public static Texture2D? GetOrCreate(UTexture2D inputTexture)
    {
        var cacheKey = inputTexture.GetPathName();
        if (_textures.TryGetValue(cacheKey, out var existingTexture))
            return existingTexture;
        
        if (inputTexture.Decode() is not { } decodedTexture) return null;
        if (decodedTexture.ToSkBitmap() is not { } bitmap) return null;
        
        var texture = new Texture2D(bitmap.Width, bitmap.Height, bitmap.Bytes, internalFormat: bitmap.ColorType switch
        {
            SKColorType.Rgba8888 => InternalFormat.Rgba8,
            SKColorType.Bgra8888 => InternalFormat.Rgba8
        }, format: bitmap.ColorType switch
        {
            SKColorType.Rgba8888 => PixelFormat.Rgba,
            SKColorType.Bgra8888 => PixelFormat.Bgra
        });
        _textures.Add(cacheKey, texture);
        texture.Generate();
        
        bitmap.Dispose();
        
        return texture;
        
    }
}