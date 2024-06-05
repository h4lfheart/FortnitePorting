using System.Collections.Generic;
using FortnitePorting.Shared.Models.Fortnite;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Models;

public record ExportMesh
{
    public string Name = string.Empty;
    public string Path = string.Empty;
    public int NumLods;
    
    public readonly List<ExportMaterial> Materials = [];
    public readonly List<ExportMaterial> OverrideMaterials = [];
}

public record ExportPart : ExportMesh
{
    [JsonIgnore] public EFortCustomGender GenderPermitted;
    
    public EFortCustomPartType Type;
}