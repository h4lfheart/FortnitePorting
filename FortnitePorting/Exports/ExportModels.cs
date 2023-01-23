using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Readers;
using Newtonsoft.Json;

namespace FortnitePorting.Exports;

public class ExportMesh
{
    public string MeshPath;
    public int NumLods;
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
    public string Part;
    public string? MorphName;
    public string? SocketName;
    public string? PoseAnimation;
    public string[]? PoseNames;

    [JsonIgnore]
    public EFortCustomGender GenderPermitted;
}

public record ExportMaterial
{
    public string MaterialName;
    public string? MasterMaterialName;
    public int SlotIndex;
    public int Hash;
    public bool IsGlass;
    public List<TextureParameter> Textures = new();
    public List<ScalarParameter> Scalars = new();
    public List<VectorParameter> Vectors = new();
}

public record ExportMaterialOverride : ExportMaterial
{
    public string? MaterialNameToSwap;
}

public record ExportMaterialParams
{
    public string MaterialToAlter;
    public int Hash;
    public List<TextureParameter> Textures = new();
    public List<ScalarParameter> Scalars = new();
    public List<VectorParameter> Vectors = new();
}

public record TextureParameter(string Name, string Value);

public record ScalarParameter(string Name, float Value);

public record VectorParameter(string Name, FLinearColor Value)
{
    public FLinearColor Value { get; set; } = Value;
}

public record TransformParameter(string Name, FTransform Value);

public class EmotePropData
{
    public string SocketName;
    public FVector LocationOffset;
    public FRotator RotationOffset;
    public FVector Scale;
    public ExportMesh? Prop;
    public string Animation;
}

public class AnimationData
{
    public string Animation;
    public string Skeleton;
    public List<EmotePropData> Props = new();
    public List<CurveData> Curves = new();
    public List<EmoteSoundData> Sounds = new();
}

public class CurveData
{
    public string Name;
    public List<CurveKey> Keys;
}
public record CurveKey(float Time, float Value);

public record EmoteSoundData(float Time, string Path, string AudioExtension);