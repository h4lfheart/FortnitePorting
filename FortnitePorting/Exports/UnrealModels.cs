using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using UAnimationAsset = CUE4Parse.UE4.Assets.Exports.Animation.UAnimationAsset;

namespace FortnitePorting.Exports;

public class UFortnitePortingCustom : UObject { }

public class UPoseAsset : UAnimationAsset
{
    public FPoseDataContainer PoseContainer;
    public bool bAdditivePose;
    public int BasePoseIndex;
    public FName RetargetSource;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        PoseContainer = GetOrDefault<FPoseDataContainer>(nameof(PoseContainer));
        bAdditivePose = GetOrDefault<bool>(nameof(bAdditivePose));
        BasePoseIndex = GetOrDefault<int>(nameof(BasePoseIndex));
        RetargetSource = GetOrDefault<FName>(nameof(RetargetSource));
    }
}

[StructFallback]
public class FPoseAssetInfluence : UFortnitePortingCustom
{
    public int BoneTransformIndex;
    public int PoseIndex;

    public FPoseAssetInfluence(FStructFallback fallback)
    {
        BoneTransformIndex = fallback.GetOrDefault<int>(nameof(BoneTransformIndex));
        PoseIndex = fallback.GetOrDefault<int>(nameof(PoseIndex));
    }
}

[StructFallback]
public class FPoseAssetInfluences : UFortnitePortingCustom
{
    public FPoseAssetInfluence[] Influences;

    public FPoseAssetInfluences(FStructFallback fallback)
    {
        Influences = fallback.GetOrDefault<FPoseAssetInfluence[]>(nameof(Influences));
    }
}

[StructFallback]
public class FPoseData : UFortnitePortingCustom
{
    public FTransform[] LocalSpacePose;
    public bool[] LocalSpacePoseMask;
    public float[] CurveData;

    public FPoseData(FStructFallback fallback)
    {
        LocalSpacePose = fallback.GetOrDefault<FTransform[]>(nameof(LocalSpacePose));
        LocalSpacePoseMask = fallback.GetOrDefault<bool[]>(nameof(LocalSpacePoseMask));
        CurveData = fallback.GetOrDefault<float[]>(nameof(CurveData));
    }
}

[StructFallback]
public class FPoseDataContainer : UFortnitePortingCustom
{
    public FSmartName[] PoseNames;
    public FName[] Tracks;
    public FPoseAssetInfluences[] TrackPoseInfluenceIndices;
    public FPoseData[] Poses;
    public Dictionary<FName, int> TrackMap;
    public FAnimCurveBase[] Curves;

    public FPoseDataContainer(FStructFallback fallback)
    {
        PoseNames = fallback.GetOrDefault<FSmartName[]>(nameof(PoseNames));
        Tracks = fallback.GetOrDefault<FName[]>(nameof(Tracks));
        TrackPoseInfluenceIndices = fallback.GetOrDefault<FPoseAssetInfluences[]>(nameof(TrackPoseInfluenceIndices));
        Poses = fallback.GetOrDefault<FPoseData[]>(nameof(Poses));
        TrackMap = fallback.GetOrDefault<Dictionary<FName, int>>(nameof(TrackMap));
        Curves = fallback.GetOrDefault<FAnimCurveBase[]>(nameof(Curves));
    }
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