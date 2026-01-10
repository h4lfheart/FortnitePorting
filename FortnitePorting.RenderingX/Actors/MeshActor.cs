using CUE4Parse.GameTypes.FN.Assets.Exports.DataAssets;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.RenderingX.Components.Mesh;
using FortnitePorting.RenderingX.Core;

namespace FortnitePorting.RenderingX.Actors;

public class MeshActor : Actor
{
    public MeshActor(UStaticMesh mesh, Transform? transform = null, List<KeyValuePair<UBuildingTextureData, int>>? textureData = null) : base(mesh.Name)
    {
        Components.Add(new StaticMeshComponent(mesh, textureData) 
        {
            Transform = transform ?? Transform.Identity
        });
    }
    
    public MeshActor(USkeletalMesh mesh, Transform? transform = null) : base(mesh.Name)
    {
        Components.Add(new SkeletalMeshComponent(mesh)
        {
           Transform = transform ?? Transform.Identity
        });
    }
}