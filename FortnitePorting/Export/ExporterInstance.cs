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
            
        exportPart.Type = part.GetOrDefault<EFortCustomPartType>("CharacterPartType").ToString();
        exportPart.AttachToSocket = part.GetOrDefault("bAttachToSocket", false);

        if (part.TryGetValue(out UObject additionalData, "AdditionalData"))
        {
            exportPart.Socket = additionalData.GetOrDefault<FName?>("AttachSocketName")?.Text;
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
        if (!AppExportOptions.ExportMaterials) return null;
        
        var hash = material.GetPathName().GetHashCode();
        if (MaterialCache.FirstOrDefault(mat => mat.Hash == hash) is { } existing)
        {
            return existing.WithSlot(index);
        }
        
        var exportMaterial = new ExportMaterial
        {
            Path = material.GetPathName(),
            Name = material.Name,
            Slot = index,
            Hash = hash
        };
        
        AccumulateParameters(material, ref exportMaterial);
        MaterialCache.Add(exportMaterial);
        return exportMaterial;
    }
    
    public void AccumulateParameters(UMaterialInterface? materialInterface, ref ExportMaterial exportMaterial)
    {
        if (materialInterface is UMaterialInstanceConstant materialInstance)
        {
            foreach (var param in materialInstance.TextureParameterValues)
            {
                if (!param.ParameterValue.TryLoad(out UTexture texture)) continue;
                exportMaterial.Textures.AddUnique(new TextureParameter(param.Name, Export(texture), texture.SRGB, texture.CompressionSettings));
            }
            
            foreach (var param in materialInstance.ScalarParameterValues)
            {
                exportMaterial.Scalars.AddUnique(new ScalarParameter(param.Name, param.ParameterValue));
            }
            
            foreach (var param in materialInstance.VectorParameterValues)
            {
                if (param.ParameterValue is null) continue;
                exportMaterial.Vectors.AddUnique(new VectorParameter(param.Name, param.ParameterValue.Value));
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
                        if (File.Exists(path)) break;
                        
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
                        if (File.Exists(path)) break;
                        
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
                        if (File.Exists(path)) break;
                        
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