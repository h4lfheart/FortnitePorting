using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.ViewModels;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.Export;

public class ExporterInstance
{
    public readonly List<Task> Tasks = new();
    private readonly HashSet<ExportMaterial> MaterialCache = new();
    private readonly ExportOptionsBase AppExportOptions;
    private readonly ExporterOptions FileExportOptions;

    public ExporterInstance(EExportType exportType)
    {
        AppExportOptions = AppSettings.Current.ExportOptions.Get(exportType);
        FileExportOptions = AppExportOptions.CreateExportOptions();
    }

    public ExportPart? CharacterPart(UObject part)
    {
        var skeletalMesh = part.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
        if (skeletalMesh is null) return null;

        var exportPart = Mesh<ExportPart>(skeletalMesh);
        if (exportPart is null) return null;
        
        if (part.TryGetValue(out FStructFallback[] materialOverrides, "MaterialOverrides"))
        {
            foreach (var material in materialOverrides)
            {
                exportPart.OverrideMaterials.AddIfNotNull(OverrideMaterial(material));
            }
        }

        exportPart.Type = part.GetOrDefault<EFortCustomPartType>("CharacterPartType").ToString();

        if (part.TryGetValue(out UObject additionalData, "AdditionalData"))
        {
            switch (additionalData.ExportType)
            {
                case "CustomCharacterHeadData":
                {
                    var meta = new ExportHeadMeta();
                    
                    foreach (var type in Enum.GetValues<ECustomHatType>())
                    {
                        if (additionalData.TryGetValue(out FName[] morphNames, type + "MorphTargets"))
                        {
                            meta.MorphNames[type] = morphNames.First().Text;
                        }
                    }
                    
                    if (additionalData.TryGetValue(out UObject skinColorSwatch, "SkinColorSwatch"))
                    {
                        var colorPairs = skinColorSwatch.GetOrDefault("ColorPairs", Array.Empty<FStructFallback>());
                        var skinColorPair = colorPairs.FirstOrDefault(x => x.Get<FName>("ColorName").Text.Equals("Skin Boost Color and Exponent", StringComparison.OrdinalIgnoreCase));
                        if (skinColorPair is not null)
                        {
                            meta.SkinColor = skinColorPair.Get<FLinearColor>("ColorValue");
                        }
                    }

                    exportPart.Meta = meta;
                    break;
                }
                case "CustomCharacterHatData":
                {
                    var meta = new ExportHatMeta
                    {
                        AttachToSocket = part.GetOrDefault("bAttachToSocket", true),
                        Socket = additionalData.GetOrDefault<FName?>("AttachSocketName")?.Text
                    };
                    
                    if (additionalData.TryGetValue(out FName hatType, "HatType"))
                    {
                        meta.HatType = hatType.Text.Replace("ECustomHatType::ECustomHatType_", string.Empty);
                    }
                    exportPart.Meta = meta;
                    break;
                }
                case "CustomCharacterCharmData":
                {
                    var meta = new ExportAttachMeta
                    {
                        AttachToSocket = part.GetOrDefault("bAttachToSocket", true),
                        Socket = additionalData.GetOrDefault<FName?>("AttachSocketName")?.Text
                    };
                    exportPart.Meta = meta;
                    break;
                }
            }
        }

        return exportPart;
    }
    
    public ExportMesh? Mesh(USkeletalMesh mesh)
    {
        return Mesh<ExportMesh>(mesh);
    }
    
    public T? Mesh<T>(USkeletalMesh mesh) where T : ExportMesh, new()
    {
        if (!mesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;
        
        var exportPart = new T
        {
            Path = Export(mesh),
            NumLods = convertedMesh.LODs.Count
        };

        var sections = convertedMesh.LODs[0].Sections.Value;
        foreach (var (index, section) in sections.Enumerate())
        {
            if (section.Material is null) continue;
            if (!section.Material.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;
            
            exportPart.Materials.AddIfNotNull(Material(material, index));
        }
        
        return exportPart;
    }

    public ExportMesh? Mesh(UStaticMesh mesh)
    {
        return Mesh<ExportMesh>(mesh);
    }

    public T? Mesh<T>(UStaticMesh mesh) where T : ExportMesh, new()
    {
        if (!mesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;
        
        var exportPart = new T
        {
            Path = Export(mesh),
            NumLods = convertedMesh.LODs.Count
        };

        var sections = convertedMesh.LODs[0].Sections.Value;
        foreach (var (index, section) in sections.Enumerate())
        {
            if (section.Material is null) continue;
            if (!section.Material.TryLoad(out var materialObject)) continue;
            if (materialObject is not UMaterialInterface material) continue;
            
            exportPart.Materials.AddIfNotNull(Material(material, index));
        }
        return exportPart;
    }

    public ExportMaterial? Material(UMaterialInterface material, int index)
    {
        return Material<ExportMaterial>(material, index);
    }

    public T? Material<T>(UMaterialInterface material, int index) where T : ExportMaterial, new()
    {
        if (!AppExportOptions.ExportMaterials) return null;
        
        var hash = material.GetPathName().GetHashCode();
        if (MaterialCache.FirstOrDefault(mat => mat.Hash == hash) is { } existing)
        {
            return existing.Copy<T>() with { Slot = index };
        }
        
        var exportMaterial = new T
        {
            Path = material.GetPathName(),
            Name = material.Name,
            Slot = index,
            Hash = hash,
            ParentName = GetAbsoluteParent(material)?.Name
        };
        
        AccumulateParameters(material, ref exportMaterial);
        MaterialCache.Add(exportMaterial);
        return exportMaterial;
    }
    
    public ExportOverrideMaterial? OverrideMaterial(FStructFallback overrideData)
    {
        var overrideMaterial = overrideData.Get<FSoftObjectPath>("OverrideMaterial");
        if (!overrideMaterial.TryLoad(out UMaterialInterface materialObject)) return null;

        var exportMaterial = Material<ExportOverrideMaterial>(materialObject, overrideData.Get<int>("MaterialOverrideIndex"));
        if (exportMaterial is null) return null;
        
        exportMaterial.MaterialNameToSwap = overrideData.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.Text.SubstringAfterLast(".");
        return exportMaterial;
    }

    public UMaterialInterface? GetAbsoluteParent(UMaterialInterface? materialInterface)
    {
        var parent = materialInterface;
        while (parent is UMaterialInstanceConstant materialInstance)
        {
            parent = materialInstance.Parent as UMaterialInterface;
        }
        return parent;
    }

    public void AccumulateParameters<T>(UMaterialInterface? materialInterface, ref T exportMaterial) where T : ExportMaterial
    {
        if (materialInterface is UMaterialInstanceConstant materialInstance)
        {
            foreach (var param in materialInstance.TextureParameterValues)
            {
                if (exportMaterial.Textures.Any(x => x.Name == param.Name)) continue;
                if (!param.ParameterValue.TryLoad(out UTexture texture)) continue;
                exportMaterial.Textures.AddUnique(new TextureParameter(param.Name, Export(texture), texture.SRGB, texture.CompressionSettings));
            }
            
            foreach (var param in materialInstance.ScalarParameterValues)
            {
                if (exportMaterial.Scalars.Any(x => x.Name == param.Name)) continue;
                exportMaterial.Scalars.AddUnique(new ScalarParameter(param.Name, param.ParameterValue));
            }
            
            foreach (var param in materialInstance.VectorParameterValues)
            {
                if (exportMaterial.Vectors.Any(x => x.Name == param.Name)) continue;
                if (param.ParameterValue is null) continue;
                exportMaterial.Vectors.AddUnique(new VectorParameter(param.Name, param.ParameterValue.Value));
            }

            if (materialInstance.StaticParameters is not null)
            {
                foreach (var param in materialInstance.StaticParameters.StaticSwitchParameters)
                {
                    if (exportMaterial.Switches.Any(x => x.Name == param.Name)) continue;
                    exportMaterial.Switches.AddUnique(new SwitchParameter(param.Name, param.Value));
                }
                
                foreach (var param in materialInstance.StaticParameters.StaticComponentMaskParameters)
                {
                    if (exportMaterial.ComponentMasks.Any(x => x.Name == param.Name)) continue;
                    
                    var color = new FLinearColor(
                        param.R ? 1.0f : 0.0f,
                        param.G ? 1.0f : 0.0f,
                        param.B ? 1.0f : 0.0f,
                        param.A ? 1.0f : 0.0f);
                    exportMaterial.ComponentMasks.AddUnique(new ComponentMaskParameter(param.Name, color));
                }
            }
            
            if (materialInstance.Parent is UMaterialInterface parentMaterial)
            {
                AccumulateParameters(parentMaterial, ref exportMaterial);
            }
        }
        else if (materialInterface is UMaterial material)
        {
            // TODO NORMAL MAT ACCUMULATION
        }
        
    }

    public string Export(UObject obj)
    {
        Tasks.Add(Task.Run(() =>
        {
            var path = string.Empty;
            try
            {
                switch (obj)
                {
                    case USkeletalMesh skeletalMesh:
                    {
                        path = GetExportPath(skeletalMesh, AppExportOptions.MeshExportType switch
                        {
                            EMeshExportTypes.UEFormat => "uemodel",
                            EMeshExportTypes.ActorX => "psk"
                        });
                        if (File.Exists(path)) return;
                        
                        var exporter = new MeshExporter(skeletalMesh, FileExportOptions);
                        exporter.TryWriteToDir(App.AssetsFolder, out var _, out var _);
                        break;
                    }
                    case UStaticMesh staticMesh:
                    {
                        path = GetExportPath(staticMesh, AppExportOptions.MeshExportType switch
                        {
                            EMeshExportTypes.UEFormat => "uemodel",
                            EMeshExportTypes.ActorX => "pskx"
                        });
                        if (File.Exists(path)) return;
                        
                        var exporter = new MeshExporter(staticMesh, FileExportOptions);
                        exporter.TryWriteToDir(App.AssetsFolder, out var _, out var _);
                        break;
                    }
                    case UTexture texture:
                    {
                        path = GetExportPath(texture, AppExportOptions.ImageType switch
                        {
                            EImageType.PNG => "png",
                            EImageType.TGA => "tga"
                        });
                        if (File.Exists(path)) return;
                        
                        switch (AppExportOptions.ImageType)
                        {
                            case EImageType.PNG:
                                File.WriteAllBytes(path, texture.Decode()!.Encode(SKEncodedImageFormat.Png, 100).ToArray());
                                break;
                            case EImageType.TGA:
                                throw new NotImplementedException("TARGA (.tga) export not currently supported.");
                        }
                        break;
                    }
                }
                
                Log.Information("Exporting {ExportType}: {Path}", obj.ExportType, path);
            }
            catch (IOException e)
            {
                Log.Warning("Failed to Export {ExportType}: {Name}", obj.ExportType, obj.Name);
                Log.Warning(e.Message + e.StackTrace);
            }
        }));
        
        return obj.GetPathName();
    }
    
    private static string GetExportPath(UObject obj, string ext, string extra = "")
    {
        var path = obj.Owner != null ? obj.Owner.Name : string.Empty;
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var directory = Path.Combine(App.AssetsFolder.FullName, path);
        Directory.CreateDirectory(directory.SubstringBeforeLast("/"));

        var finalPath = directory + $"{extra}.{ext.ToLower()}";
        return finalPath;
    }
}