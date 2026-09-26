using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Rendering.Actors;
using FortnitePorting.Rendering.Extensions;

namespace FortnitePorting.Rendering.Animation.Props;

public sealed class AnimPropAttachment
{
    public required MeshActor Actor { get; init; }
    public required string SocketName { get; init; }
    public required FVector LocationOffset { get; init; }
    public required FRotator RotationOffset { get; init; }
    public required FVector Scale { get; init; }

    public void UpdateTransform(MasterSkeletonPose masterSkeletonPose)
    {
        if (!masterSkeletonPose.TryGetSocketTransform(SocketName, out var socketTransform))
            return;

        var attachmentScale = Scale.Equals(FVector.ZeroVector) ? FVector.OneVector : Scale;
        var offsetTransform = new FTransform(RotationOffset.Quaternion(), LocationOffset, attachmentScale);
        offsetTransform.Normalize();
        socketTransform.Normalize();

        var composedTransform = offsetTransform * socketTransform;
        composedTransform.Normalize();
        Actor.MeshComponent.Transform = composedTransform;
    }
}
