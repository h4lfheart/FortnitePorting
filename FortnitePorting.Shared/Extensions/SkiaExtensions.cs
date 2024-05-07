using CUE4Parse.UE4.Objects.Core.Math;
using SkiaSharp;

namespace FortnitePorting.Shared.Extensions;

public static class SkiaExtensions
{
    public static SKShader RadialGradient(int size, params SKColor[] colors)
    {
        return SKShader.CreateRadialGradient(new SKPoint(size / 2f, size / 2f), size / 5.0f * 4, colors, SKShaderTileMode.Clamp);
    }

    public static SKShader RadialGradient(int size, params FLinearColor[] colors)
    {
        return RadialGradient(size, colors.Select(col => SKColor.Parse(col.Hex)).ToArray());
    }
    
    public static SKShader LinearGradient(int size, bool horizontal, params SKColor[] colors)
    {
        var start = new SKPoint(0, 0);
        var end = horizontal ? new SKPoint(size, 0) : new SKPoint(0, size);
        return SKShader.CreateLinearGradient(start, end, colors, SKShaderTileMode.Clamp);
    }

    public static SKShader LinearGradient(int size, bool horizontal, params FLinearColor[] colors)
    {
        return LinearGradient(size, horizontal, colors.Select(col => SKColor.Parse(col.Hex)).ToArray());
    }
}