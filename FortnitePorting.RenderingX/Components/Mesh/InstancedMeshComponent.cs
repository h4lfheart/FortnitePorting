using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Components.Mesh;

public class InstancedMeshComponent : SpatialComponent
{
    public UStaticMesh Mesh;
    
    public InstancedMeshComponent(UStaticMesh mesh) : base(mesh.Name)
    {
        Mesh = mesh;
    }
}