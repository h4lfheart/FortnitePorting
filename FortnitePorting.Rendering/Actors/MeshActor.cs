using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.Rendering.Animation.Montage;
using FortnitePorting.Rendering.Animation.Pose;
using FortnitePorting.Rendering.Components.Animation;
using FortnitePorting.Rendering.Components.Mesh;
using FortnitePorting.Rendering.Core;
using FortnitePorting.Rendering.Exceptions;

namespace FortnitePorting.Rendering.Actors;

public class MeshActor : Actor
{
    public MeshComponent MeshComponent;

    private readonly AnimationPropComponent? _animationPropComponent;

    public MeshActor(UStaticMesh mesh, Transform? transform = null, List<KeyValuePair<UBuildingTextureData, int>>? textureData = null) : base(mesh.Name)
    {
        MeshComponent = new StaticMeshComponent(mesh, textureData)
        {
            Transform = transform ?? Transform.Identity
        };

        Components.Add(MeshComponent);
    }

    public MeshActor(USkeletalMesh mesh, Transform? transform = null, UAnimationAsset? animation = null) : base(mesh.Name)
    {
        var skeletalMeshComponent = new SkeletalMeshComponent(mesh, animation)
        {
            Transform = transform ?? Transform.Identity
        };
        MeshComponent = skeletalMeshComponent;

        _animationPropComponent = new AnimationPropComponent(skeletalMeshComponent);
        Components.Add(MeshComponent);
        Components.Add(_animationPropComponent);

        if (animation is not null)
            _animationPropComponent.Attach(animation);
    }

    public SkeletalPoseEvaluator? Pose =>
        MeshComponent is SkeletalMeshComponent skeletalMeshComponent
            ? skeletalMeshComponent.Renderer.Pose
            : null;

    public float Time => Pose?.Time ?? 0f;
    public float Duration => Pose?.Duration ?? 0f;
    public bool IsPlaying => Pose?.IsPlaying ?? false;
    public bool HasAnimation => Pose?.Sequence is not null;

    public string? CurrentSectionName => Pose?.CurrentSectionName;
    public List<AnimMontageSection> Sections => Pose?.MontageSections ?? [];

    public void Play(UAnimationAsset animation, bool loop = true, float speed = 1f)
    {
        if (MeshComponent is not SkeletalMeshComponent skeletalMeshComponent)
            throw new RenderingXException("Animation requires a skeletal mesh actor.");

        skeletalMeshComponent.Play(animation, loop, speed);
        _animationPropComponent?.Attach(animation, loop, speed);
    }

    public void Stop()
    {
        if (MeshComponent is not SkeletalMeshComponent skeletalMeshComponent)
            throw new RenderingXException("Animation requires a skeletal mesh actor.");

        _animationPropComponent?.Clear();
        skeletalMeshComponent.Stop();
    }

    public void Pause()
    {
        if (MeshComponent is not SkeletalMeshComponent skeletalMeshComponent)
            throw new RenderingXException("Animation requires a skeletal mesh actor.");

        skeletalMeshComponent.Pause();
        _animationPropComponent?.Pause();
    }

    public void Resume()
    {
        if (MeshComponent is not SkeletalMeshComponent skeletalMeshComponent)
            throw new RenderingXException("Animation requires a skeletal mesh actor.");

        skeletalMeshComponent.Resume();
        _animationPropComponent?.Resume();
    }

    public void Seek(float timeSeconds)
    {
        if (MeshComponent is not SkeletalMeshComponent skeletalMeshComponent)
            throw new RenderingXException("Animation requires a skeletal mesh actor.");

        skeletalMeshComponent.Seek(timeSeconds);
        _animationPropComponent?.Seek(timeSeconds);
    }

    public void JumpToSection(int index)
    {
        if (MeshComponent is not SkeletalMeshComponent skeletalMeshComponent)
            throw new RenderingXException("Animation requires a skeletal mesh actor.");

        skeletalMeshComponent.JumpToSection(index);
        _animationPropComponent?.JumpToSection(index);
    }

    public void JumpToSection(string name)
    {
        if (MeshComponent is not SkeletalMeshComponent skeletalMeshComponent)
            throw new RenderingXException("Animation requires a skeletal mesh actor.");

        skeletalMeshComponent.JumpToSection(name);
        _animationPropComponent?.JumpToSection(name);
    }
}
