using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Export;

public record ExportMesh
{
    public string Path;
    public int NumLods;
    public FVector Location = FVector.ZeroVector;
    public FRotator Rotation = FRotator.ZeroRotator;
    public FVector Scale = FVector.OneVector;
    public List<ExportMaterial> Materials = new();
    public List<ExportMaterial> OverrideMaterials = new();
    public List<ExportTextureData> TextureData = new();
    public List<ExportMesh> Children = new();
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
    public Dictionary<ECustomHatType, string> MorphNames = new();
    public FLinearColor SkinColor;
}

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
    public int Hash;
    public List<TextureParameter> Textures = new();
    public List<ScalarParameter> Scalars = new();
    public List<VectorParameter> Vectors = new();
    public List<SwitchParameter> Switches = new();
    public List<ComponentMaskParameter> ComponentMasks = new();
}

public record ExportMaterial : ExportParameterContainer
{
    public string Path;
    public string Name;
    public string? AbsoluteParent;
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