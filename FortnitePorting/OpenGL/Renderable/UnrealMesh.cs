using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Renderable;

public class UnrealMesh : IRenderable
{
    public Matrix4 Transform;
    private List<UnrealSection> Sections = new();
    
    public UnrealMesh(USkeletalMesh skeletalMesh)
    {
        if (!skeletalMesh.TryConvert(out var convertedMesh)) return;
        
        var lod = convertedMesh.LODs[0];
        var sections = lod.Sections.Value;
        Sections.AddRange(sections.Select(x => new UnrealSection(lod, x, skeletalMesh.Materials[x.MaterialIndex]?.Load<UMaterialInterface>())
        {
            Transform = Transform
        }));
    }
    
    public UnrealMesh(UStaticMesh staticMesh)
    {
        if (!staticMesh.TryConvert(out var convertedMesh)) return;
        
        var lod = convertedMesh.LODs[0];
        var sections = lod.Sections.Value;
        Sections.AddRange(sections.Select(x => new UnrealSection(lod, x, staticMesh.Materials[x.MaterialIndex]?.Load<UMaterialInterface>())
        {
            Transform = Transform
        }));
    }

    public void Setup()
    {
        Sections.ForEach(x=> x.Setup());
    }

    public void Render(Camera camera)
    {
        Sections.ForEach(x=> x.Render(camera));
    }
    
    public void Dispose()
    {
        Sections.ForEach(x => x.Dispose());
    }
}