using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using FortnitePorting.Rendering.Renderers;

namespace FortnitePorting.Rendering.Components.Mesh;

public class SkeletalMeshComponent(USkeletalMesh mesh) : MeshComponent(new SkeletalMeshRenderer(mesh));