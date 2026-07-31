using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using FortnitePorting.Rendering.Renderers;

namespace FortnitePorting.Rendering.Components.Mesh;

public class SkeletalMeshComponent : MeshComponent
{
    public new SkeletalMeshRenderer Renderer => (SkeletalMeshRenderer) base.Renderer;

    public USkeletalMesh Mesh { get; }

    public SkeletalMeshComponent(USkeletalMesh mesh, UAnimationAsset? animation = null)
        : base(new SkeletalMeshRenderer(mesh, animation))
    {
        Mesh = mesh;
    }

    public void Play(UAnimationAsset animation, bool loop = true, float speed = 1f)
    {
        Renderer.Play(animation, Mesh, loop, speed);
    }

    public void Stop() => Renderer.Stop();
}
