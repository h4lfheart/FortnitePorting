using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.RenderingX.Managers;

namespace FortnitePorting.RenderingX.Components.Mesh;

public class InstancedMeshComponent(UStaticMesh mesh) : Component
{
    public UStaticMesh Mesh = mesh;
}