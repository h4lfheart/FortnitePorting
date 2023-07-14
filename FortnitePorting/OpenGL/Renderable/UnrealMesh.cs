using System.Collections.Generic;
using System.Linq;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.Math;
using OpenTK.Mathematics;

namespace FortnitePorting.OpenGL.Renderable;

public class UnrealMesh : IRenderable
{
    public FBox Bounds;
    private readonly List<UnrealSection> Sections = new();

    public UnrealMesh(USkeletalMesh skeletalMesh, Matrix4? transform = null)
    {
        Log.Information("Loading Skeletal Mesh: {Name}", skeletalMesh.Name);
        if (!skeletalMesh.TryConvert(out var convertedMesh))
        {
            Log.Error("Failed to Load Skeletal Mesh: {Name}", skeletalMesh.Name);
            return;
        }

        Bounds = convertedMesh.BoundingBox;
        var lod = convertedMesh.LODs[0];
        var sections = lod.Sections.Value;
        Sections.AddRange(sections.Select(x => new UnrealSection(lod, x, skeletalMesh.Materials[x.MaterialIndex]?.Load<UMaterialInterface>(), transform)));
    }

    public UnrealMesh(UStaticMesh staticMesh, Matrix4? transform = null)
    {
        Log.Information("Loading Static Mesh: {Name}", staticMesh.Name);
        if (!staticMesh.TryConvert(out var convertedMesh))
        {
            Log.Error("Failed to Load Static Mesh: {Name}", staticMesh.Name);
            return;
        }

        Bounds = convertedMesh.BoundingBox;
        var lod = convertedMesh.LODs[0];
        var sections = lod.Sections.Value;
        Sections.AddRange(sections.Select(x => new UnrealSection(lod, x, staticMesh.Materials[x.MaterialIndex]?.Load<UMaterialInterface>(), transform)));
    }

    public void Setup()
    {
        Sections.ForEach(x => x.Setup());
    }

    public void Render(Camera camera)
    {
        Sections.ForEach(x => x.Render(camera));
    }

    public void Dispose()
    {
        Sections.ForEach(x => x.Dispose());
    }
}