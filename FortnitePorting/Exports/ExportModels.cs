using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace FortnitePorting.Exports;

public class ExportMesh
{
    public string MeshPath;
    public FVector Offset = FVector.ZeroVector;
    public FVector Scale = FVector.OneVector;
    public List<ExportMaterial> Materials = new();
    public List<ExportMaterial> OverrideMaterials = new();
}


public class ExportMeshOverride : ExportMesh
{
    public string MeshToSwap;
}
public class ExportPart : ExportMesh
{
    public string? Part;
    public string? MorphName;
    public string? SocketName;
}

public record ExportMaterial
{
    public string MaterialName;
    public int SlotIndex;
    public string? MaterialNameToSwap;
    public int Hash;
    public List<TextureParameter> Textures = new();
    public List<ScalarParameter> Scalars = new();
    public List<VectorParameter> Vectors = new();
}

public record TextureParameter(string Name, string Value);

public record ScalarParameter(string Name, float Value);

public record VectorParameter(string Name, FLinearColor Value);

public class EmotePropData
{
    public string SocketName;
    public FVector LocationOffset;
    public FRotator RotationOffset;
    public FVector Scale;
    public ExportMesh? Prop;
    public string Animation;
}