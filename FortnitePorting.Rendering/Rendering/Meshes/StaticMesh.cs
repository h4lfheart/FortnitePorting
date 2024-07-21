using CUE4Parse_Conversion.Meshes.PSK;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Rendering.Rendering.Meshes;

public class StaticMesh : Mesh
{
    public StaticMesh(UStaticMesh staticMesh, Materials.Material[]? materials = null) : this(staticMesh, staticMesh.Convert().LODs[0], materials) { }
    
    public StaticMesh(UStaticMesh staticMesh, CStaticMeshLod lod, Materials.Material[]? materials = null) : base(lod, lod.Verts, staticMesh.Materials)
    {
        if (materials is not null)
            Materials = materials;
    }
}