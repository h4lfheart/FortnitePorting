using System.Linq;
using CUE4Parse.UE4.Objects.Core.Math;
using SkiaSharp;

namespace FortnitePorting.Extensions;

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
}