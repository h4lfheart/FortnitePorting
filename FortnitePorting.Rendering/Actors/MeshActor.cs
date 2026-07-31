using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
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
}
