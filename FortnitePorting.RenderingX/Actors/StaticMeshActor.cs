using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.RenderingX.Components.Rendering;
using FortnitePorting.RenderingX.Core;
using FortnitePorting.RenderingX.Exceptions;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Actors;

public class StaticMeshActor : WorldActor
{
    public void SetStaticMesh(UStaticMesh mesh)
    {
        if (!mesh.TryConvert(out var convertedMesh))
        {
            throw new RenderingException("Failed to convert static mesh.");
        }
        
        AddComponent(new MeshRendererComponent(new StaticMeshRenderer(convertedMesh)));
    }

}