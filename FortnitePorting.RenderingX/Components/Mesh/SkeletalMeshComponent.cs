using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using FortnitePorting.RenderingX.Renderers;

namespace FortnitePorting.RenderingX.Components.Mesh;

public class SkeletalMeshComponent(USkeletalMesh mesh) : MeshComponent(new SkeletalMeshRenderer(mesh));