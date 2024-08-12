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
    [UProperty] public FName SocketName;
    [UProperty] public FVector LocationOffset = FVector.ZeroVector;
    [UProperty] public FRotator RotationOffset = FRotator.ZeroRotator;
    [UProperty] public FVector Scale = FVector.OneVector;
    [UProperty] public bool bInheritScale;
    [UProperty] public UStaticMesh? StaticMeshProp;
    [UProperty] public USkeletalMesh? SkeletalMeshProp;
    [UProperty] public UAnimSequence? SkeletalMeshPropAnimation;
    [UProperty] public UAnimMontage? SkeletalMeshPropMontage;
}