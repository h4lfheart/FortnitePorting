using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Actors;

public class InstancedMeshActor : Actor
{
    public InstancedMeshActor(UStaticMesh mesh, Transform transform) : base(mesh.Name)
    {
        Components.Add(new InstancedMeshComponent(mesh) 
        {
            Transform = transform
        });
    }
}