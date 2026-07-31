using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.Rendering.Components.Mesh;
using FortnitePorting.Rendering.Core;
using FortnitePorting.Rendering.Exceptions;

namespace FortnitePorting.Rendering.Actors;

public class MeshActor : Actor
{
    public MeshComponent MeshComponent;
    
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
        MeshComponent = new SkeletalMeshComponent(mesh, animation)
        {
            Transform = transform ?? Transform.Identity
        };
        
        Components.Add(MeshComponent);
    }

    public void Play(UAnimationAsset animation, bool loop = true, float speed = 1f)
    {
        if (MeshComponent is not SkeletalMeshComponent skeletal)
            throw new RenderingXException("Animation requires a skeletal mesh actor.");

        skeletal.Play(animation, loop, speed);
    }

    public void Stop()
    {
        if (MeshComponent is not SkeletalMeshComponent skeletal)
            throw new RenderingXException("Animation requires a skeletal mesh actor.");

        skeletal.Stop();
    }
}
