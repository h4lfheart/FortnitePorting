using System;
using System.Collections.Generic;
using System.Linq;
using ConcurrentCollections;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Material.Editor;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Exporting.Context;

public partial class ExportContext
{
    private ConcurrentHashSet<ExportMaterial> MaterialCache = [];
    
    public ExportMaterial? Material(UMaterialInterface material, int index)
    {
        if (!Meta.Settings.ExportMaterials) return null;

        var hash = material.GetPathName().GetHashCode();
        if (MaterialCache.FirstOrDefault(mat => mat.Hash == hash) is { } existing) return existing with { Slot = index};

        var exportMaterial = new ExportMaterial
        {
            Path = material.GetPathName(),
            Name = material.Name,
            Slot = index,
            Hash = hash
        };

        AccumulateParameters(material, ref exportMaterial);

        exportMaterial.OverrideBlendMode = (material as UMaterialInstanceConstant)?.BasePropertyOverrides?.BlendMode ?? exportMaterial.BaseBlendMode;

        MaterialCache.Add(exportMaterial);
        return exportMaterial;
    }
    
    public ExportMaterial? OverrideMaterial(FStructFallback overrideData)
    {
        var overrideMaterial = overrideData.Get<FSoftObjectPath>("OverrideMaterial");
        if (!overrideMaterial.TryLoad(out UMaterialInterface materialObject)) return null;

        var material = Material(materialObject, overrideData.Get<int>("MaterialOverrideIndex"));
        return material;
    }
    
    public ExportOverrideMaterial? OverrideMaterialSwap(FStructFallback overrideData)
    {
        var overrideMaterial = overrideData.Get<FSoftObjectPath>("OverrideMaterial");
        if (!overrideMaterial.TryLoad(out UMaterialInterface materialObject)) return null;

        var exportMaterial = Material(materialObject, overrideData.Get<int>("MaterialOverrideIndex"));
        if (exportMaterial is null) return null;

        return new ExportOverrideMaterial
        {
            Material = exportMaterial,
            MaterialNameToSwap = overrideData.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.Text.SubstringAfterLast(".")
        };
    }
    
    public List<ExportOverrideParameters> OverrideParameters(FStructFallback overrideData)
    {
        var materialsToAlter = new List<FSoftObjectPath>();
        if (overrideData.TryGetValue<FSoftObjectPath>(out var alterMaterial, "MaterialToAlter"))
            materialsToAlter.AddIfNotNull(alterMaterial);
        
        if (overrideData.TryGetValue<FSoftObjectPath[]>(out var alterMaterials, "MaterialsToAlter"))
            materialsToAlter.AddRangeIfNotNull(alterMaterials);

        materialsToAlter.RemoveAll(mat =>
            mat.AssetPathName.IsNone || string.IsNullOrWhiteSpace(mat.AssetPathName.Text));

        var exportParametersSet = new List<ExportOverrideParameters>();
        foreach (var materialToAlter in materialsToAlter)
        {
            var exportParams = new ExportOverrideParameters();
            AccumulateParameters(overrideData, ref exportParams);

            exportParams.MaterialNameToAlter = materialToAlter.AssetPathName.Text.SubstringAfterLast(".");
            exportParams.Hash = exportParams.GetHashCode();
            exportParametersSet.Add(exportParams);
        }

       
        return exportParametersSet;
    }
    
    public void AccumulateParameters<T>(UMaterialInterface? materialInterface, ref T parameterCollection) where T : ParameterCollection
    {
        if (materialInterface is UMaterialInstanceConstant materialInstance)
        {
            foreach (var param in materialInstance.TextureParameterValues)
            {
                if (parameterCollection.Textures.Any(x => x.Name.Equals(param.Name))) continue;
                if (!param.ParameterValue.TryLoad(out UTexture texture)) continue;
                var embeddedAsset = materialInstance.Owner != null 
                                    && texture.Owner != null 
                                    && materialInstance.Owner.Name.Equals(texture.Owner.Name);
                parameterCollection.Textures.AddUnique(new TextureParameter(param.Name, 
                    new ExportTexture(Export(texture, embeddedAsset: embeddedAsset), texture.SRGB, texture.CompressionSettings)));
            }

            foreach (var param in materialInstance.ScalarParameterValues)
            {
                if (parameterCollection.Scalars.Any(x => x.Name.Equals(param.Name))) continue;
                parameterCollection.Scalars.AddUnique(new ScalarParameter(param.Name, param.ParameterValue));
            }

            foreach (var param in materialInstance.VectorParameterValues)
            {
                if (parameterCollection.Vectors.Any(x => x.Name.Equals(param.Name))) continue;
                if (param.ParameterValue is null) continue;
                parameterCollection.Vectors.AddUnique(new VectorParameter(param.Name, param.ParameterValue.Value));
            }

            if (materialInstance.StaticParameters is not null)
            {
                foreach (var param in materialInstance.StaticParameters.StaticSwitchParameters)
                {
                    if (parameterCollection.Switches.Any(x => x.Name.Equals(param.Name))) continue;
                    parameterCollection.Switches.AddUnique(new SwitchParameter(param.Name, param.Value));
                }

                foreach (var param in materialInstance.StaticParameters.StaticComponentMaskParameters)
                {
                    if (parameterCollection.ComponentMasks.Any(x => x.Name.Equals(param.Name))) continue;

                    parameterCollection.ComponentMasks.AddUnique(new ComponentMaskParameter(param.Name, param.ToLinearColor()));
                }
            }
            
            if (materialInstance.TryLoadEditorData<UMaterialInstanceEditorOnlyData>(out var materialInstanceEditorData) && materialInstanceEditorData?.StaticParameters is not null)
            {
                foreach (var parameter in materialInstanceEditorData.StaticParameters.StaticSwitchParameters)
                {
                    if (parameter.ParameterInfo is null) continue;
                    parameterCollection.Switches.AddUnique(new SwitchParameter(parameter.Name, parameter.Value));
                }

                foreach (var parameter in materialInstanceEditorData.StaticParameters.StaticComponentMaskParameters)
                {
                    if (parameter.ParameterInfo is null) continue;
                    parameterCollection.ComponentMasks.AddUnique(new ComponentMaskParameter(parameter.Name, parameter.ToLinearColor()));
                }
            }

            if (materialInstance.Parent is UMaterialInterface parentMaterial) AccumulateParameters(parentMaterial, ref parameterCollection);
        }
        else if (materialInterface is UMaterial material)
        {
            if (parameterCollection is ExportMaterial exportMaterial)
            {
                exportMaterial.BaseMaterial = material;
                exportMaterial.PhysMaterialName =
                    material.GetOrDefault<FPackageIndex?>("PhysMaterial")?.Name ?? string.Empty;
            }

            if (parameterCollection.Textures.Count == 0 && !material.Name.Contains("Parent", StringComparison.OrdinalIgnoreCase))
            {
                AccumulateParameters(material, ref parameterCollection);
            }
        }
    }
    
    public void AccumulateParameters<T>(UMaterial material, ref T parameterCollection) where T : ParameterCollection
    {
        // TODO use uefn data and custom FPackageIndex resolver to start reading material tree 
        var parameters = new CMaterialParams2();
        material.GetParams(parameters, EMaterialFormat.AllLayers);
                
        foreach (var (name, value) in parameters.Textures)
        {
            if (value is not UTexture2D texture) continue;
            
            parameterCollection.Textures.AddUnique(new TextureParameter(name, new ExportTexture(Export(texture), texture.SRGB, texture.CompressionSettings)));
        }
    }
    
    public void AccumulateParameters<T>(FStructFallback data, ref T parameterCollection) where T : ParameterCollection
    {
        var textureParams = data.GetOrDefault<FStyleParameter<FSoftObjectPath>[]>("TextureParams");
        foreach (var param in textureParams)
        {
            if (parameterCollection.Textures.Any(x => x.Name == param.Name)) continue;
            if (!param.Value.TryLoad(out UTexture texture)) continue;
            parameterCollection.Textures.AddUnique(new TextureParameter(param.Name, new ExportTexture(Export(texture), texture.SRGB, texture.CompressionSettings)));
        }

        var floatParams = data.GetOrDefault<FStyleParameter<float>[]>("FloatParams");
        foreach (var param in floatParams)
        {
            if (parameterCollection.Scalars.Any(x => x.Name == param.Name)) continue;
            parameterCollection.Scalars.AddUnique(new ScalarParameter(param.Name, param.Value));
        }

        var colorParams = data.GetOrDefault<FStyleParameter<FLinearColor>[]>("ColorParams");
        foreach (var param in colorParams)
        {
            if (parameterCollection.Vectors.Any(x => x.Name == param.ParamName.Text)) continue;
            parameterCollection.Vectors.AddUnique(new VectorParameter(param.Name, param.Value));
        }
    }
}