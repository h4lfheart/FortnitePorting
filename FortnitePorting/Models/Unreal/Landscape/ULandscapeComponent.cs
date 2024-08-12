using System;
using System.Runtime.InteropServices.JavaScript;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Models.Unreal.Landscape;

public class ULandscapeComponent : USceneComponent
{
    [UProperty] public int SectionBaseX;
    [UProperty] public int SectionBaseY;
    [UProperty] public int ComponentSizeQuads;
    [UProperty] public int SubsectionSizeQuads;
    [UProperty] public int NumSubsections;
    [UProperty] public FVector4 HeightmapScaleBias = FVector4.ZeroVector;
    [UProperty] public UTexture2D HeightmapTexture;
    
    [UProperty] public FVector4 WeightmapScaleBias = FVector4.ZeroVector;
    [UProperty] public float WeightmapSubsectionOffset;
    [UProperty] public FWeightmapLayerAllocationInfo[] WeightmapLayerAllocations = [];
    
    [UProperty] public UTexture2D[] WeightmapTextures = [];
    
    public void GetExtent(ref int minX, ref int minY, ref int maxX, ref int maxY)
    {
        minX = Math.Min(SectionBaseX, minX);
        minY = Math.Min(SectionBaseY, minY);
        maxX = Math.Max(SectionBaseX + ComponentSizeQuads, maxX);
        maxY = Math.Max(SectionBaseY + ComponentSizeQuads, maxY);
    }

}