using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Material.Editor;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Export.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models.CUE4Parse;
using FortnitePorting.Shared.Models.Fortnite;
using FortnitePorting.Shared.Services;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.Export;

public class ExportContext
{
    private List<Task> ExportTasks = [];
    private HashSet<ExportMaterial> MaterialCache = [];

    private readonly HybridFileProvider Provider;
    
    private readonly ExportMetaData AppExportOptions;
    private readonly ExporterOptions FileExportOptions;

    public ExportContext(HybridFileProvider provider, ExportMetaData metaData)
    {
        Provider = provider;
        AppExportOptions = metaData;
        FileExportOptions = AppExportOptions.Settings.CreateExportOptions();
    }
    
    public ExportPart? CharacterPart(UObject part)
    {
        var skeletalMesh = part.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
        if (skeletalMesh is null) return null;

        var exportPart = Mesh<ExportPart>(skeletalMesh);
        if (exportPart is null) return null;
        
        exportPart.Type = part.GetOrDefault<EFortCustomPartType>("CharacterPartType");
        exportPart.GenderPermitted = part.GetOrDefault("GenderPermitted", EFortCustomGender.Male);

        if (part.TryGetValue(out FStructFallback[] materialOverrides, "MaterialOverrides"))
        {
            foreach (var material in materialOverrides)
            {
                exportPart.OverrideMaterials.AddIfNotNull(OverrideMaterial(material));
            }
        }
        
        return exportPart;
    }
    
    public T? Mesh<T>(USkeletalMesh? mesh) where T : ExportMesh, new()
    {
        if (mesh is null) return null;
        if (!mesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;

        var exportPart = new T
        {
            Name = mesh.Name,
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
        if (!AppExportOptions.Settings.ExportMaterials) return null;

        var hash = material.GetPathName().GetHashCode();
        if (MaterialCache.FirstOrDefault(mat => mat.Hash == hash) is { } existing) return existing with { Slot = index};

        var exportMaterial = new ExportMaterial
        {
            Path = material.GetPathName(),
            Name = material.Name,
            Slot = index,
            Hash = hash,
        };

        AccumulateParameters(material, ref exportMaterial);

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
    
    
    public void AccumulateParameters<T>(UMaterialInterface? materialInterface, ref T parameterCollection) where T : ParameterCollection
    {
        if (materialInterface is UMaterialInstanceConstant materialInstance)
        {
            foreach (var param in materialInstance.TextureParameterValues)
            {
                if (parameterCollection.Textures.Any(x => x.Name.Equals(param.Name))) continue;
                if (!param.ParameterValue.TryLoad(out UTexture texture)) continue;
                parameterCollection.Textures.AddUnique(new TextureParameter(param.Name, Export(texture), texture.SRGB, texture.CompressionSettings));
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
            
            // TODO optional segment loading
            /*if (materialInstance.TryLoadEditorData<UMaterialInstanceEditorOnlyData>(out var materialInstanceEditorData) && materialInstanceEditorData?.StaticParameters is not null)
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
            }*/

            if (materialInstance.Parent is UMaterialInterface parentMaterial) AccumulateParameters(parentMaterial, ref parameterCollection);
        }
        else if (materialInterface is UMaterial material)
        {
            if (parameterCollection is ExportMaterial exportMaterial)
            {
                exportMaterial.BaseMaterialPath = material.GetPathName();
            }
            
            // todo UMaterial parsing
        }
    }

    public async Task<string> ExportAsync(UObject asset, bool returnRealPath = false, bool synchronousExport = false)
    {
        var extension = asset switch
        {
            USkeletalMesh or UStaticMesh or USkeleton => AppExportOptions.Settings.MeshFormat switch
            {
                EMeshFormat.UEFormat => "uemodel",
                EMeshFormat.ActorX => "psk",
                EMeshFormat.Gltf2 => "glb",
                EMeshFormat.OBJ => "obj",
            },
            UAnimSequence => AppExportOptions.Settings.AnimFormat switch
            {
                EAnimFormat.UEFormat => "ueanim",
                EAnimFormat.ActorX => "psa"
            },
            UTexture => AppExportOptions.Settings.ImageFormat switch
            {
                EImageFormat.PNG => "png",
                EImageFormat.TGA => "tga"
            },
            USoundWave => "wav"
        };

        var path = GetExportPath(asset, extension);
        Log.Information("Exporting {ExportType}: {Path}", asset.ExportType, path);
        
        var returnValue = returnRealPath ? path : asset.GetPathName();
        if (File.Exists(path)) return returnValue;

        var exportTask = TaskService.Run(() =>
        {
            try
            {
                Export(asset, path);
            }
            catch (IOException e)
            {
                Log.Warning("Failed to Export {ExportType}: {Name}", asset.ExportType, asset.Name);
                Log.Warning(e.Message + e.StackTrace);
            }
        });
        
        ExportTasks.Add(exportTask);

        if (synchronousExport)
            exportTask.Wait();

        return returnValue;
    }
    
    public string Export(UObject asset, bool returnRealPath = false, bool synchronousExport = false)
    {
        return ExportAsync(asset, returnRealPath, synchronousExport).GetAwaiter().GetResult();
    }

    private void Export(UObject asset, string path)
    {
        var assetsFolder = new DirectoryInfo(AppExportOptions.AssetsRoot);
        switch (asset)
        {
            case USkeletalMesh skeletalMesh:
            {
                var exporter = new MeshExporter(skeletalMesh, FileExportOptions);
                exporter.TryWriteToDir(assetsFolder, out _, out _);
                break;
            }
            case UStaticMesh staticMesh:
            {
                var exporter = new MeshExporter(staticMesh, FileExportOptions);
                exporter.TryWriteToDir(assetsFolder, out _, out _);
                break;
            }
            case USkeleton skeleton:
            {
                var exporter = new MeshExporter(skeleton, FileExportOptions);
                exporter.TryWriteToDir(assetsFolder, out _, out _);
                break;
            }
            case UAnimSequence animSequence:
            {
                var exporter = new AnimExporter(animSequence, FileExportOptions);
                exporter.TryWriteToDir(assetsFolder, out _, out _);
                break;
            }
            case UTexture texture:
            {
                using var fileStream = File.OpenWrite(path);
                var textureBitmap = texture.Decode();
                switch (AppExportOptions.Settings.ImageFormat)
                {
                    case EImageFormat.PNG:
                    {
                        textureBitmap?.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fileStream); 
                        break;
                    }
                    case EImageFormat.TGA:
                    {
                        throw new NotImplementedException("TARGA (.tga) export not currently supported.");
                    }
                }

                break;
            }
            case USoundWave soundWave:
            {
                if (!SoundExtensions.TrySaveSoundToPath(soundWave, path))
                {
                    throw new Exception($"Failed to export sound '{soundWave.Name}' at {path}");
                }
                
                break;
            }
        }
    }
    
    public string GetExportPath(UObject obj, string ext)
    {
        var path = obj.Owner != null ? obj.Owner.Name : string.Empty;
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var directory = Path.Combine(AppExportOptions.AssetsRoot, path);
        Directory.CreateDirectory(directory.SubstringBeforeLast("/"));

        var finalPath = $"{directory}.{ext.ToLower()}";
        return finalPath;
    }
}