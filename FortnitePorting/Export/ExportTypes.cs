using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace FortnitePorting.Export;

public record ExportMesh
{
    public string Name;
    public string Path;
    public int NumLods;
    public FVector Location = FVector.ZeroVector;
    public FRotator Rotation = FRotator.ZeroRotator;
    public FVector Scale = FVector.OneVector;
    public readonly List<ExportMaterial> Materials = [];
    public readonly List<ExportMaterial> OverrideMaterials = [];
    public readonly List<ExportTextureData> TextureData = [];
    public readonly List<ExportMesh> Children = [];
}

public record ExportPart : ExportMesh
{
    public string Type;
    public ExportPartMeta Meta = new();
}

public class ExportPartMeta
{
}

public class ExportHeadMeta : ExportPartMeta
{
    public readonly Dictionary<ECustomHatType, string> MorphNames = new();
    public FLinearColor SkinColor;
    public List<PoseData> PoseData = [];
}

public class PoseData
{
    public string Name;
    public List<PoseKey> Keys = new List<PoseKey>();

    public PoseData(string name)
    {
        Name = name;
    }
}

public record PoseKey(string Name, FVector Location, FQuat Rotation, FVector Scale);

public class ExportAttachMeta : ExportPartMeta
{
    public bool AttachToSocket;
    public string? Socket;
}

public class ExportHatMeta : ExportAttachMeta
{
    public string HatType;
}

public record ExportParameterContainer
{
    [JsonIgnore] public UMaterial? AbsoluteParentMaterial;
    
    public int Hash;
    public string? AbsoluteParent;
    public List<TextureParameter> Textures = [];
    public List<ScalarParameter> Scalars = [];
    public List<VectorParameter> Vectors = [];
    public List<SwitchParameter> Switches = [];
    public List<ComponentMaskParameter> ComponentMasks = [];
}

public record ExportMaterial : ExportParameterContainer
{
    public string Path;
    public string Name;
    // TODO do the logic for this upon import, not export
    public bool UseGlassMaterial;
    public bool UseFoliageMaterial;
    public bool IsTransparent;
    public int Slot;
    
    public T Copy<T>() where T : ExportMaterial, new()
    {
        var mat = new T
        {
            Path = Path,
            Name = Name,
            AbsoluteParent = AbsoluteParent,
            Slot = Slot,
            Hash = Hash,
            Textures = Textures,
            Scalars = Scalars,
            Vectors = Vectors,
            Switches = Switches,
            ComponentMasks = ComponentMasks
        };
        return mat;
    }
}

public record ExportOverrideMaterial : ExportMaterial
{
    public string? MaterialNameToSwap;
}

public record ExportOverrideParameters : ExportParameterContainer
{
    public string MaterialNameToAlter;
}


public record ExportTextureData
{
    public int Hash;
    public TextureParameter? Diffuse;
    public TextureParameter? Normal;
    public TextureParameter? Specular;
}

public record TextureParameter(string Name, string Value, bool sRGB, TextureCompressionSettings CompressionSettings);

public record ScalarParameter(string Name, float Value);

public record VectorParameter(string Name, FLinearColor Value);

public record SwitchParameter(string Name, bool Value);

public record ComponentMaskParameter(string Name, FLinearColor Value);


public class ExportAnimSection
{
    public string Path;
    public string Name;
    public float Time;
    public float Length;
    public float LinkValue;
    public bool Loop;
}

public class ExportSound
{
    public string Path;
    public float Time;
    public bool Loop;
}

public class ExportProp
{
    public ExportMesh Mesh;
    public List<ExportAnimSection> AnimSections;
    public string SocketName;
    public FVector LocationOffset;
    public FRotator RotationOffset;
    public FVector Scale;
}