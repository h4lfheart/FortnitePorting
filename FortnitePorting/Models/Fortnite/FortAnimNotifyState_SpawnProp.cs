using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Fortnite;

public class FortAnimNotifyState_SpawnProp : UObject
{
    public FName SocketName;
    public FVector LocationOffset;
    public FRotator RotationOffset;
    public FVector Scale;
    public bool bInheritScale;
    public UStaticMesh? StaticMeshProp;
    public USkeletalMesh? SkeletalMeshProp;
    public UAnimSequence? SkeletalMeshPropAnimation;
    public UAnimMontage? SkeletalMeshPropMontage;

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