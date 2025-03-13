using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.Rendering.Extensions;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Rendering.Rendering.Meshes;

public class SkeletalMesh : Mesh
{
    public SkeletalMesh(USkeletalMesh skeletalMesh, Materials.Material[]? materials = null) : this(skeletalMesh, skeletalMesh.Convert().LODs[0], materials) { }
    
    public SkeletalMesh(USkeletalMesh skeletalMesh, CSkelMeshLod lod, Materials.Material[]? materials = null) : base(lod, lod.Verts, skeletalMesh.Materials)
    {
        if (materials is not null)
            Materials = materials;
    }
}