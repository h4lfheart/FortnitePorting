using System.Collections.Generic;

namespace FortnitePorting.Export;

public class ExportPart
{
    public string MeshPath;
    public List<ExportMaterial> Materials = new();
    public List<ExportMaterial> OverrideMaterials = new();
}

public record ExportMaterial
{
    public string MaterialName;
    public int SlotIndex;
    public List<TextureParameter> Textures = new();
}

public record TextureParameter(string Name, string Path);