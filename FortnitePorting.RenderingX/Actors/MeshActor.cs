using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Actors;

public class MeshActor : Actor
{
    public MeshActor(UStaticMesh mesh, Transform transform) : base(mesh.Name)
    {
        Components.Add(new StaticMeshComponent(mesh) 
        {
            Transform = transform
        });
    }
    
    public MeshActor(USkeletalMesh mesh, Transform transform) : base(mesh.Name)
    {
        Components.Add(new SkeletalMeshComponent(mesh)
        {
           Transform = transform
        });
    }
}