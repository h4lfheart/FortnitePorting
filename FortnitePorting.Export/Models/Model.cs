using FortnitePorting.Shared.Models.Fortnite;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Models;

public record Model
{
    public string Name = string.Empty;
    public string Path = string.Empty;
    public int NumLods;
    
    public readonly List<Material> Materials = [];
    public readonly List<Material> OverrideMaterials = [];
}

public record Part : Model
{
    [JsonIgnore] public EFortCustomGender GenderPermitted;
    [JsonIgnore] public EFortCustomPartType CharacterPartType;
    
    public string Type => CharacterPartType.ToString();
}