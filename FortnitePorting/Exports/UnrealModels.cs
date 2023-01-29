using System.Collections.Generic;
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
    public FTransform[] RetargetSourceAssetReferencePose;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        PoseContainer = GetOrDefault<FPoseDataContainer>(nameof(PoseContainer));
        bAdditivePose = GetOrDefault<bool>(nameof(bAdditivePose));
        BasePoseIndex = GetOrDefault<int>(nameof(BasePoseIndex));
        RetargetSource = GetOrDefault<FName>(nameof(RetargetSource));
        RetargetSourceAssetReferencePose = GetOrDefault<FTransform[]>(nameof(RetargetSourceAssetReferencePose));
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

public class UAnimMontage : UAnimCompositeBase
{
    public FCompositeSection[] CompositeSections;
    public FSlotAnimationTrack[] SlotAnimTracks;
    public FAnimNotify[] Notifies;
    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        CompositeSections = GetOrDefault<FCompositeSection[]>(nameof(CompositeSections));
        SlotAnimTracks = GetOrDefault<FSlotAnimationTrack[]>(nameof(SlotAnimTracks));
        Notifies = GetOrDefault<FAnimNotify[]>(nameof(Notifies));
    }
}

[StructFallback]
public class FCompositeSection : UFortnitePortingCustom
{
    public FName SectionName;
    public FName NextSectionName;
    public float SegmentBeginTime;
    public float SegmentLength;
    public UAnimSequence? LinkedSequence;
    
    public FCompositeSection(FStructFallback fallback)
    {
        SectionName = fallback.GetOrDefault<FName>(nameof(SectionName));
        NextSectionName = fallback.GetOrDefault<FName>(nameof(NextSectionName));
        SegmentBeginTime = fallback.GetOrDefault<float>(nameof(SegmentBeginTime));
        LinkedSequence = fallback.GetOrDefault<UAnimSequence>(nameof(LinkedSequence));
        SegmentLength = fallback.GetOrDefault<float>(nameof(SegmentLength));
    }
}

[StructFallback]
public class FAnimNotify : UFortnitePortingCustom
{
    public float TriggerTimeOffset;
    public FPackageIndex NotifyStateClass;
    public float Duration;
    public UAnimSequence LinkedSequence;
    
    public FAnimNotify(FStructFallback fallback)
    {
        TriggerTimeOffset = fallback.GetOrDefault<float>(nameof(TriggerTimeOffset));
        NotifyStateClass = fallback.GetOrDefault<FPackageIndex>(nameof(NotifyStateClass));
        Duration = fallback.GetOrDefault<float>(nameof(Duration));
    }
}

public class UFortSoundNodeLicensedContentSwitcher : USoundNode
{
    
}

[StructFallback]
public class FSlotAnimationTrack : UFortnitePortingCustom
{
    public FName SlotName;
    public FAnimTrack AnimTrack;
    
    public FSlotAnimationTrack(FStructFallback fallback)
    {
        SlotName = fallback.GetOrDefault<FName>(nameof(SlotName));
        AnimTrack = fallback.GetOrDefault<FAnimTrack>(nameof(AnimTrack));
    }
}

[StructFallback]
public class FAnimTrack : UFortnitePortingCustom
{
    public FAnimSegment[] AnimSegments;
    
    public FAnimTrack(FStructFallback fallback)
    {
        AnimSegments = fallback.GetOrDefault<FAnimSegment[]>(nameof(AnimSegments));
    }
}

[StructFallback]
public class FAnimSegment : UFortnitePortingCustom
{
    public float AnimEndTime;
    public float AnimPlayRate;
    public UAnimSequence AnimReference;
    public float AnimStartTime;
    public int LoopingCount;
    public float StartPos;
    public FAnimSegment(FStructFallback fallback)
    {
        AnimEndTime = fallback.GetOrDefault<float>(nameof(AnimEndTime));
        AnimPlayRate = fallback.GetOrDefault<float>(nameof(AnimPlayRate));
        AnimReference = fallback.GetOrDefault<UAnimSequence>(nameof(AnimReference));
        AnimStartTime = fallback.GetOrDefault<float>(nameof(AnimStartTime));
        LoopingCount = fallback.GetOrDefault<int>(nameof(LoopingCount));
        StartPos = fallback.GetOrDefault<float>(nameof(StartPos));
    }
}
