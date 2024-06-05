using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace FortnitePorting.Export.Models;

public record ParameterCollection
{
    public List<TextureParameter> Textures = [];
    public List<ScalarParameter> Scalars = [];
    public List<VectorParameter> Vectors = [];
    public List<SwitchParameter> Switches = [];
    public List<ComponentMaskParameter> ComponentMasks = [];
}

public record ExportMaterial : ParameterCollection
{
    public string Name = string.Empty;
    public string Path = string.Empty;
    public string BaseMaterialPath = string.Empty;
    public int Slot;
    public int Hash;
}

public record TextureParameter(string Name, string Value, bool sRGB, TextureCompressionSettings CompressionSettings);

public record ScalarParameter(string Name, float Value);

public record VectorParameter(string Name, FLinearColor Value);

public record SwitchParameter(string Name, bool Value);

public record ComponentMaskParameter(string Name, FLinearColor Value);
