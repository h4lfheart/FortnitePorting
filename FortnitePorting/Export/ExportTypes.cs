using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;

namespace FortnitePorting.Export;

public class ExportMesh
{
    public string Path;
    public int NumLods;
    public FVector Location = FVector.ZeroVector;
    public FRotator Rotation = FRotator.ZeroRotator;
    public FVector Scale = FVector.OneVector;
    public List<ExportMaterial> Materials = new();
    public List<ExportMaterial> OverrideMaterials = new();
}

public class ExportPart : ExportMesh
{
    public string Type;
    public ExportPartMeta Meta = new();
}

public class ExportPartMeta { }

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

public record ExportMaterial
{
    public string Path;
    public string Name;
    public string? ParentName;
    public int Slot;
    public int Hash;
        
    public List<TextureParameter> Textures = new();
    public List<ScalarParameter> Scalars = new();
    public List<VectorParameter> Vectors = new();
    public List<SwitchParameter> Switches = new();
    public List<ComponentMaskParameter> ComponentMasks = new();

    public T Copy<T>() where T : ExportMaterial, new()
    {
        var mat = new T
        {
            Path = Path,
            Name = Name,
            ParentName = ParentName,
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
    
public record TextureParameter(string Name, string Value, bool sRGB, TextureCompressionSettings CompressionSettings);

public record ScalarParameter(string Name, float Value);

public record VectorParameter(string Name, FLinearColor Value);

public record SwitchParameter(string Name, bool Value);

public record ComponentMaskParameter(string Name, FLinearColor Value);