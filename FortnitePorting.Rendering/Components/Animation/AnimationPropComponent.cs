using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using FortnitePorting.CUE4Parse.Models.Fortnite.AnimNotifies;
using FortnitePorting.Rendering.Actors;
using FortnitePorting.Rendering.Animation.Props;
using FortnitePorting.Rendering.Components.Mesh;

namespace FortnitePorting.Rendering.Components.Animation;

public class AnimationPropComponent : Component
{
    private readonly SkeletalMeshComponent _skeletalMeshComponent;
    private MasterSkeletonPose? _masterSkeletonPose;
    private readonly List<AnimPropAttachment> _propAttachments = [];

    public AnimationPropComponent(SkeletalMeshComponent skeletalMeshComponent) : base(nameof(AnimationPropComponent))
    {
        _skeletalMeshComponent = skeletalMeshComponent;
    }

    public void Attach(UAnimationAsset animation, bool loop = true, float speed = 1f)
    {
        Clear();

        if (Actor is null)
            return;

        var spawnProps = CollectSpawnProps(animation);
        if (spawnProps.Count == 0)
            return;

        var animationSkeleton = animation.Skeleton.Load<USkeleton>()
                                ?? _skeletalMeshComponent.Mesh.Skeleton.Load<USkeleton>();
        if (animationSkeleton is null)
            return;

        _masterSkeletonPose = MasterSkeletonPose.Create(animationSkeleton, _skeletalMeshComponent.Mesh);
        _masterSkeletonPose.Play(animation, animationSkeleton, loop, speed);

        foreach (var spawnProp in spawnProps)
        {
            var propMeshActor = CreatePropMeshActor(spawnProp);
            if (propMeshActor is null)
                continue;

            Actor.Children.Add(propMeshActor);

            if (spawnProp.Animation is not null && propMeshActor.MeshComponent is SkeletalMeshComponent propSkeletalMeshComponent)
                propSkeletalMeshComponent.Play(spawnProp.Animation, loop, speed);

            var attachment = new AnimPropAttachment
            {
                Actor = propMeshActor,
                SocketName = spawnProp.SocketName,
                LocationOffset = spawnProp.LocationOffset,
                RotationOffset = spawnProp.RotationOffset,
                Scale = spawnProp.Scale
            };
            attachment.UpdateTransform(_masterSkeletonPose);
            _propAttachments.Add(attachment);
        }

        _skeletalMeshComponent.Renderer.AfterAnimationUpdate += UpdateAttachments;
    }

    public void Clear()
    {
        _skeletalMeshComponent.Renderer.AfterAnimationUpdate -= UpdateAttachments;

        if (Actor is not null)
        {
            foreach (var attachment in _propAttachments)
                Actor.Children.Remove(attachment.Actor);
        }

        _propAttachments.Clear();
        _masterSkeletonPose?.Stop();
        _masterSkeletonPose = null;
    }

    public void Pause()
    {
        _masterSkeletonPose?.Pause();
        ForEachPropSkeletal(component => component.Pause());
    }

    public void Resume()
    {
        _masterSkeletonPose?.Resume();
        ForEachPropSkeletal(component => component.Resume());
    }

    public void Seek(float timeSeconds)
    {
        _masterSkeletonPose?.Seek(timeSeconds);
        ForEachPropSkeletal(component => component.Seek(timeSeconds));
        UpdateAttachmentTransforms();
    }

    public void JumpToSection(int index)
    {
        _masterSkeletonPose?.JumpToSection(index);
        ForEachPropSkeletal(component => component.Seek(0f));
        UpdateAttachmentTransforms();
    }

    public void JumpToSection(string name)
    {
        _masterSkeletonPose?.JumpToSection(name);
        ForEachPropSkeletal(component => component.Seek(0f));
        UpdateAttachmentTransforms();
    }

    private void UpdateAttachments(float deltaTime)
    {
        if (_masterSkeletonPose is null)
            return;

        _masterSkeletonPose.Update(deltaTime);
        UpdateAttachmentTransforms();
    }

    private void UpdateAttachmentTransforms()
    {
        if (_masterSkeletonPose is null)
            return;

        foreach (var attachment in _propAttachments)
            attachment.UpdateTransform(_masterSkeletonPose);
    }

    private void ForEachPropSkeletal(Action<SkeletalMeshComponent> action)
    {
        foreach (var attachment in _propAttachments)
        {
            if (attachment.Actor.MeshComponent is SkeletalMeshComponent skeletalMeshComponent)
                action(skeletalMeshComponent);
        }
    }

    private static MeshActor? CreatePropMeshActor(AnimSpawnPropInfo spawnProp)
    {
        if (spawnProp.StaticMesh is not null)
            return new MeshActor(spawnProp.StaticMesh);

        if (spawnProp.SkeletalMesh is not null)
            return new MeshActor(spawnProp.SkeletalMesh);

        return null;
    }

    private static List<AnimSpawnPropInfo> CollectSpawnProps(UAnimationAsset animation)
    {
        var spawnProps = new List<AnimSpawnPropInfo>();

        foreach (var notify in EnumerateNotifies(animation))
        {
            if (notify.NotifyStateClass.Load() is not FortAnimNotifyState_SpawnProp spawnPropNotify)
                continue;

            var spawnPropInfo = CreateSpawnPropInfo(spawnPropNotify);
            if (spawnPropInfo is not null)
                spawnProps.Add(spawnPropInfo);
        }

        return spawnProps;
    }

    private static AnimSpawnPropInfo? CreateSpawnPropInfo(FortAnimNotifyState_SpawnProp spawnPropNotify)
    {
        if (spawnPropNotify.StaticMeshProp is null && spawnPropNotify.SkeletalMeshProp is null)
            return null;

        UAnimationAsset? propAnimation = spawnPropNotify.SkeletalMeshPropMontage;
        propAnimation ??= spawnPropNotify.SkeletalMeshPropAnimationMontage;
        propAnimation ??= spawnPropNotify.SkeletalMeshPropAnimation;

        return new AnimSpawnPropInfo
        {
            SocketName = spawnPropNotify.SocketName.Text,
            StaticMesh = spawnPropNotify.StaticMeshProp,
            SkeletalMesh = spawnPropNotify.SkeletalMeshProp,
            Animation = propAnimation,
            LocationOffset = spawnPropNotify.LocationOffset,
            RotationOffset = spawnPropNotify.RotationOffset,
            Scale = spawnPropNotify.Scale
        };
    }

    private static IEnumerable<FAnimNotifyEvent> EnumerateNotifies(UAnimationAsset animation)
    {
        switch (animation)
        {
            case UAnimMontage montage:
            {
                foreach (var notify in montage.Notifies)
                    yield return notify;

                foreach (var notify in EnumerateMontageSectionNotifies(montage))
                    yield return notify;
                break;
            }
            case UAnimSequenceBase sequence:
            {
                foreach (var notify in sequence.Notifies)
                    yield return notify;
                break;
            }
        }
    }

    private static IEnumerable<FAnimNotifyEvent> EnumerateMontageSectionNotifies(UAnimMontage montage)
    {
        if (montage.CompositeSections is not { Length: > 0 })
            yield break;

        var visitedSectionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var currentSection = montage.CompositeSections[0];

        while (true)
        {
            if (!visitedSectionNames.Add(currentSection.SectionName.Text))
                yield break;

            var linkedSequence = currentSection.LinkedSequence.Load<UAnimSequence>();
            if (linkedSequence?.Notifies is { Length: > 0 } notifies)
            {
                foreach (var notify in notifies)
                    yield return notify;
            }

            var isLoopingSection = currentSection.SectionName == currentSection.NextSectionName || currentSection.NextSectionName.IsNone;
            if (isLoopingSection)
                yield break;

            var nextSection = montage.CompositeSections.FirstOrDefault(section => currentSection.NextSectionName == section.SectionName);
            if (nextSection is null)
                yield break;

            currentSection = nextSection;
        }
    }
}
