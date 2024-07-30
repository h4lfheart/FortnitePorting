using System;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Models.Unreal.Landscape;

public static class LandscapeDataAccess
{
    private const int MaxValue = 65535;
    private const float MidValue = 32768f;

    private const float LANDSCAPE_ZSCALE = 1.0f / 128.0f;
    private const float LANDSCAPE_INV_ZSCALE = 128.0f;
    
    private const float LANDSCAPE_XYOFFSET_SCALE = 1.0f / 256.0f;
    private const float LANDSCAPE_INV_XYOFFSET_SCALE = 256.0f;
    
    public static float GetLocalHeight(ushort height)
    {
        return (height - MidValue) * LANDSCAPE_ZSCALE;
    }
    
    public static float UnpackHeight(FColor inHeightmapSample)
    {
        var height = (ushort) ((inHeightmapSample.R << 8) + inHeightmapSample.G);
        return GetLocalHeight(height);
    }
    
    public static FVector UnpackNormal(FColor inHeightmapSample)
    {
        var normal = new FVector();
        normal.X = 2f * inHeightmapSample.B / 255f - 1f;
        normal.Y = 2f * inHeightmapSample.A / 255f - 1f;
        normal.Z = MathF.Sqrt(MathF.Max(1.0f - (MathF.Pow(normal.X, 2) + MathF.Pow(normal.Y, 2)), 0.0f));
        return normal;
    }
}