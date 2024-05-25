using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Unreal;

[StructFallback]
public class FWeightmapLayerAllocationInfo
{
    public FPackageIndex LayerInfo;
    public byte WeightmapTextureIndex;
    public byte WeightmapTextureChannel;

    public FWeightmapLayerAllocationInfo(FStructFallback fallback)
    {
        LayerInfo = fallback.GetOrDefault<FPackageIndex>(nameof(LayerInfo));
        WeightmapTextureIndex = fallback.GetOrDefault<byte>(nameof(WeightmapTextureIndex));
        WeightmapTextureChannel = fallback.GetOrDefault<byte>(nameof(WeightmapTextureChannel));
    }
}