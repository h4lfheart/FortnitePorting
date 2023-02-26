using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Exports;

public class UFortnitePortingCustom : UObject
{
}

public class FortAnimNotifyState_SpawnProp : UFortnitePortingCustom
{
    public FName SocketName { get; private set; }
    public FVector LocationOffset { get; private set; }
    public FRotator RotationOffset { get; private set; }
    public FVector Scale { get; private set; }
    public bool bInheritScale { get; private set; }
    public UStaticMesh? StaticMeshProp { get; private set; }
    public USkeletalMesh? SkeletalMeshProp { get; private set; }
    public UAnimSequence? SkeletalMeshPropAnimation { get; private set; }
    public UAnimMontage? SkeletalMeshPropMontage { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        SocketName = GetOrDefault<FName>(nameof(SocketName));
        LocationOffset = GetOrDefault(nameof(LocationOffset), FVector.ZeroVector);
        RotationOffset = GetOrDefault(nameof(RotationOffset), FRotator.ZeroRotator);
        Scale = GetOrDefault(nameof(Scale), FVector.OneVector);
        bInheritScale = GetOrDefault<bool>(nameof(bInheritScale));
        StaticMeshProp = GetOrDefault<UStaticMesh>(nameof(StaticMeshProp));
        SkeletalMeshProp = GetOrDefault<USkeletalMesh>(nameof(SkeletalMeshProp));
        SkeletalMeshPropAnimation = GetOrDefault<UAnimSequence>(nameof(SkeletalMeshPropAnimation));
        SkeletalMeshPropMontage = GetOrDefault<UAnimMontage>(nameof(SkeletalMeshPropAnimation));
    }
}

public class FortAnimNotifyState_EmoteSound : UFortnitePortingCustom
{
    public USoundCue? EmoteSound1P { get; private set; }
    public USoundCue? EmoteSound3P { get; private set; }

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        EmoteSound1P = GetOrDefault<USoundCue>(nameof(EmoteSound1P));
        EmoteSound3P = GetOrDefault<USoundCue>(nameof(EmoteSound3P));
    }
}

public class UFortSoundNodeLicensedContentSwitcher : USoundNode
{
}

[StructFallback]
public class FWeightmapLayerAllocationInfo
{
    public ULandscapeLayerInfoObject LayerInfo;
    public byte WeightmapTextureIndex;
    public byte WeightmapTextureChannel;

    public FWeightmapLayerAllocationInfo(FStructFallback fallback)
    {
        LayerInfo = fallback.GetOrDefault<ULandscapeLayerInfoObject>(nameof(LayerInfo));
        WeightmapTextureIndex = fallback.GetOrDefault<byte>(nameof(WeightmapTextureIndex));
        WeightmapTextureChannel = fallback.GetOrDefault<byte>(nameof(WeightmapTextureChannel));
    }
}

public class ULandscapeLayerInfoObject : UFortnitePortingCustom
{
    public FName LayerName;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        LayerName = GetOrDefault<FName>(nameof(LayerName));
    }
}