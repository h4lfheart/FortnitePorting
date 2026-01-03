using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Exceptions;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Actors;

public class SkeletalMeshActor : WorldActor
{
    public void SetSkeletalMesh(USkeletalMesh mesh)
    {
        RemoveComponent<MeshRendererComponent>();
        AddComponent(new MeshRendererComponent(new SkeletalMeshRenderer(mesh)));
    }

}