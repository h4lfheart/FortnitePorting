using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Shared.Models.Fortnite;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Models;

public record ExportObject
{
    public string Name = string.Empty;
    public FVector Location = FVector.ZeroVector;
    public FRotator Rotation = FRotator.ZeroRotator;
    public FVector Scale = FVector.OneVector;
}

public record ExportMesh : ExportObject
{
    public string Path = string.Empty;
    public int NumLods;
    public bool IsEmpty;

    public readonly List<ExportMaterial> Materials = [];
    public readonly List<ExportMaterial> OverrideMaterials = [];
    public readonly List<ExportTextureData> TextureData = [];
    public readonly List<ExportMesh> Children = [];
    public readonly List<ExportTransform> Instances = [];
    public ExportLightCollection Lights = new();

    public void AddChildren(IEnumerable<ExportObject> objects)
    {
        foreach (var obj in objects)
        {
            if (obj is ExportMesh exportMesh)
            {
                Children.Add(exportMesh);
            }
            else if (obj is ExportLight exportLight)
            {
                Lights.Add(exportLight);
            }
        }
    }
}

public record ExportPart : ExportMesh
{
    [JsonIgnore] public EFortCustomGender GenderPermitted;
    
    public EFortCustomPartType Type;
    public BaseMeta Meta = new();
}


public record ExportTransform(FTransform transform)
{
    public FVector Location = transform.Translation;
    public FRotator Rotation = transform.Rotation.Rotator();
    public FVector Scale = transform.Scale3D;
}

