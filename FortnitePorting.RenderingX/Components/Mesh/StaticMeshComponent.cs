using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Components.Mesh;

public class StaticMeshComponent(UStaticMesh mesh) : MeshComponent(new StaticMeshRenderer(mesh));