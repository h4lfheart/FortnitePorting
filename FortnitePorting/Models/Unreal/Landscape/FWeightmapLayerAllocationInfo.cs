using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Unreal.Landscape;

[StructFallback]
public class FWeightmapLayerAllocationInfo
{
    [UProperty] public FPackageIndex LayerInfo;
    [UProperty] public byte WeightmapTextureIndex;
    [UProperty] public byte WeightmapTextureChannel;
}