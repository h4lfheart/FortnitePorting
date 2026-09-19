using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace FortnitePorting.Exporting.Models;

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
    public string BaseMaterialPath => BaseMaterial?.GetPathName() ?? string.Empty;
    public int Slot;
    public int Hash;

    public string PhysMaterialName;
    public EBlendMode OverrideBlendMode;
    public EBlendMode BaseBlendMode => BaseMaterial?.BlendMode ?? EBlendMode.BLEND_Opaque;
    public ETranslucencyLightingMode TranslucencyLightingMode => BaseMaterial?.TranslucencyLightingMode ?? ETranslucencyLightingMode.TLM_VolumetricDirectional;
    public EMaterialShadingModel ShadingModel => BaseMaterial?.ShadingModel ?? EMaterialShadingModel.MSM_DefaultLit;

    [JsonIgnore] public UMaterial? BaseMaterial;
}

public record ExportOverrideMaterial
{
    public ExportMaterial Material;
    public string MaterialNameToSwap;
}

public record ExportOverrideParameters : ParameterCollection
{
    public string MaterialNameToAlter;
    public int Hash;
}

public record ExportOverrideMorphTargets(string Name, float Value);

public record TextureParameter(string Name, ExportTexture Texture);

public record ScalarParameter(string Name, float Value);

public record VectorParameter(string Name, FLinearColor Value);

public record SwitchParameter(string Name, bool Value);

public record ComponentMaskParameter(string Name, FLinearColor Value);

public record ExportTextureData
{
    public string Path;
    public int Hash;
    public int Index;
    public ExportTexture? Diffuse;
    public ExportTexture? Normal;
    public ExportTexture? Specular;
    public ExportMaterial? OverrideMaterial;
}

