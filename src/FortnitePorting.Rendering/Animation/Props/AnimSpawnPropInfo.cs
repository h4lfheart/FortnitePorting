using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Rendering.Animation.Props;

public sealed class AnimSpawnPropInfo
{
    public required string SocketName { get; init; }
    public UStaticMesh? StaticMesh { get; init; }
    public USkeletalMesh? SkeletalMesh { get; init; }
    public UAnimationAsset? Animation { get; init; }
    public required FVector LocationOffset { get; init; }
    public required FRotator RotationOffset { get; init; }
    public required FVector Scale { get; init; }
}
