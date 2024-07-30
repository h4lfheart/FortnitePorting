using System;
using System.Runtime.InteropServices.JavaScript;
using CUE4Parse.UE4.Assets.Exports.Component;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Models.Unreal.Landscape;

public class ULandscapeComponent : USceneComponent
{
    public int SectionBaseX;
    public int SectionBaseY;
    public int ComponentSizeQuads;
    public int SubsectionSizeQuads;
    public int NumSubsections;
    public FVector4 HeightmapScaleBias;
    public UTexture2D HeightmapTexture;
    
    public FVector4 WeightmapScaleBias;
    public float WeightmapSubsectionOffset;
    public FWeightmapLayerAllocationInfo[] WeightmapLayerAllocations;

    public UTexture2D[] WeightmapTextures;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        SectionBaseX = GetOrDefault<int>(nameof(SectionBaseX));
        SectionBaseY = GetOrDefault<int>(nameof(SectionBaseY));
        
        ComponentSizeQuads = GetOrDefault<int>(nameof(ComponentSizeQuads));
        SubsectionSizeQuads = GetOrDefault<int>(nameof(SubsectionSizeQuads));
        
        NumSubsections = GetOrDefault<int>(nameof(NumSubsections));
        
        HeightmapScaleBias = GetOrDefault(nameof(HeightmapScaleBias), FVector4.ZeroVector);
        HeightmapTexture = GetOrDefault<UTexture2D>(nameof(HeightmapTexture));
        
        WeightmapScaleBias = GetOrDefault(nameof(WeightmapScaleBias), FVector4.ZeroVector);
        WeightmapSubsectionOffset = GetOrDefault<float>(nameof(WeightmapSubsectionOffset));
        WeightmapLayerAllocations = GetOrDefault(nameof(WeightmapLayerAllocations), Array.Empty<FWeightmapLayerAllocationInfo>());
        WeightmapTextures = GetOrDefault(nameof(WeightmapTextures), Array.Empty<UTexture2D>());
    }
    
    public void GetExtent(ref int minX, ref int minY, ref int maxX, ref int maxY)
    {
        minX = Math.Min(SectionBaseX, minX);
        minY = Math.Min(SectionBaseY, minY);
        maxX = Math.Max(SectionBaseX + ComponentSizeQuads, maxX);
        maxY = Math.Max(SectionBaseY + ComponentSizeQuads, maxY);
    }

}