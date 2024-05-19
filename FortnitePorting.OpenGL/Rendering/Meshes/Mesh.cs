using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using FortnitePorting.OpenGL.Rendering.Levels;
using OpenTK.Mathematics;
using Serilog;

namespace FortnitePorting.OpenGL.Rendering.Meshes;

public class Mesh : IRenderable
{
    public Matrix4 Transform = Matrix4.Identity;
    
    private readonly List<Section> Sections = [];

    public Mesh(USkeletalMesh skeletalMesh, Matrix4? transform = null)
    {
        if (!skeletalMesh.TryConvert(out var convertedMesh)) return;

        Transform = transform ?? Matrix4.Identity;
        var lod = convertedMesh.LODs[0];
        var sections = lod.Sections.Value;
        Sections.AddRange(sections.Select(x => new Section(lod, x, skeletalMesh.Materials[x.MaterialIndex]?.Load<UMaterialInterface>(), transform)));
    }

    public Mesh(UStaticMesh staticMesh, TextureData? textureData = null, Matrix4? transform = null)
    {
        if (!staticMesh.TryConvert(out var convertedMesh)) return;

        Transform = transform ?? Matrix4.Identity;
        
        var lod = convertedMesh.LODs[0];
        var sections = lod.Sections.Value;
        Sections.AddRange(sections.Select(x => new Section(lod, x, staticMesh.Materials[x.MaterialIndex]?.Load<UMaterialInterface>(), textureData, transform)));
    }

    public void Setup()
    {
        Sections.ForEach(section => section.Setup());
    }

    public void Render(Camera camera)
    {
        Sections.ForEach(section =>
        {
            section.Transform = Transform;
            section.Render(camera);
        });
    }

    public void Dispose()
    {
        Sections.ForEach(section => section.Dispose());
    }
}