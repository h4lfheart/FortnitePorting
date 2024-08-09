using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;
using FortnitePorting.Shared.Models.Fortnite;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Models;

public record ExportMesh
{
    public string Name = string.Empty;
    public string Path = string.Empty;
    public int NumLods;
    public bool IsEmpty;
    
    public FVector Location = FVector.ZeroVector;
    public FRotator Rotation = FRotator.ZeroRotator;
    public FVector Scale = FVector.OneVector;
    
    public readonly List<ExportMaterial> Materials = [];
    public readonly List<ExportMaterial> OverrideMaterials = [];
    public readonly List<ExportTextureData> TextureData = [];
    public readonly List<ExportMesh> Children = [];
    public readonly List<ExportTransform> Instances = [];
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

