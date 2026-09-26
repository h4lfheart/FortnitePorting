using CUE4Parse_Conversion.Dto;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.Rendering.Animation.Pose;
using FortnitePorting.Rendering.Extensions;

namespace FortnitePorting.Rendering.Animation.Props;

public sealed class MasterSkeletonPose
{
    private readonly SkeletalPoseEvaluator _poseEvaluator;
    private readonly SkeletonSocketMap _socketMap;

    private MasterSkeletonPose(SkeletalPoseEvaluator poseEvaluator, SkeletonSocketMap socketMap)
    {
        _poseEvaluator = poseEvaluator;
        _socketMap = socketMap;
    }

    public static MasterSkeletonPose Create(USkeleton skeleton, USkeletalMesh? mesh = null)
    {
        using var converted = new SkeletonDto(skeleton);
        var poseEvaluator = new SkeletalPoseEvaluator(converted.Bones, skeleton.ReferenceSkeleton.FinalRefBonePose);
        var socketMap = SkeletonSocketMap.FromSkeleton(skeleton, mesh);
        return new MasterSkeletonPose(poseEvaluator, socketMap);
    }

    public void Play(UAnimationAsset animation, USkeleton animationSkeleton, bool loop = true, float speed = 1f)
        => _poseEvaluator.Play(animation, animationSkeleton, loop, speed);

    public void Stop() => _poseEvaluator.Stop();

    public void Pause() => _poseEvaluator.Pause();

    public void Resume() => _poseEvaluator.Resume();

    public void Seek(float timeSeconds) => _poseEvaluator.Seek(timeSeconds);

    public void JumpToSection(int index) => _poseEvaluator.JumpToSection(index);

    public void JumpToSection(string name) => _poseEvaluator.JumpToSection(name);

    public void Update(float deltaTime) => _poseEvaluator.Update(deltaTime);

    public bool TryGetSocketTransform(string socketName, out FTransform transform)
    {
        if (_socketMap.TryGetSocket(socketName, out var socketBinding))
        {
            if (!_poseEvaluator.TryGetBoneTransform(socketBinding.BoneName, out var boneTransform))
            {
                transform = FTransform.Identity;
                return false;
            }

            var relativeTransform = (FTransform) socketBinding.Relative.Clone();
            relativeTransform.Normalize();
            boneTransform.Normalize();
            transform = relativeTransform * boneTransform;
            return true;
        }

        if (_poseEvaluator.TryGetBoneTransform(socketName, out transform))
            return true;

        transform = FTransform.Identity;
        return false;
    }

    private readonly record struct SkeletonSocketBinding(string SocketName, string BoneName, FTransform Relative);

    private sealed class SkeletonSocketMap
    {
        private readonly Dictionary<string, SkeletonSocketBinding> _bindingsByName;

        public SkeletonSocketMap(IEnumerable<SkeletonSocketBinding> socketBindings)
        {
            _bindingsByName = new Dictionary<string, SkeletonSocketBinding>(StringComparer.OrdinalIgnoreCase);
            foreach (var socketBinding in socketBindings)
                _bindingsByName[socketBinding.SocketName] = socketBinding;
        }

        public static SkeletonSocketMap FromSkeleton(USkeleton skeleton, USkeletalMesh? mesh = null)
        {
            var socketBindings = new List<SkeletonSocketBinding>();
            AddSockets(socketBindings, skeleton.Sockets);

            if (mesh?.Sockets is { Length: > 0 } meshSockets)
                AddSockets(socketBindings, meshSockets);

            return new SkeletonSocketMap(socketBindings);
        }

        public bool TryGetSocket(string name, out SkeletonSocketBinding binding)
            => _bindingsByName.TryGetValue(name, out binding);

        private static void AddSockets(List<SkeletonSocketBinding> socketBindings, FPackageIndex[] socketPackageIndices)
        {
            foreach (var socketPackageIndex in socketPackageIndices)
            {
                if (socketPackageIndex.Load<USkeletalMeshSocket>() is not { } skeletalMeshSocket)
                    continue;

                var relativeTransform = new FTransform(
                    skeletalMeshSocket.RelativeRotation.Quaternion(),
                    skeletalMeshSocket.RelativeLocation,
                    skeletalMeshSocket.RelativeScale);

                socketBindings.Add(new SkeletonSocketBinding(
                    skeletalMeshSocket.SocketName.Text,
                    skeletalMeshSocket.BoneName.Text,
                    relativeTransform));
            }
        }
    }
}
