using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using SkiaSharp;

namespace FortnitePorting.Export;

public static class ExportHelpers
{
    public static void CharacterParts(IEnumerable<UObject> inputParts, List<ExportPart> exportParts)
    {
        foreach (var part in inputParts)
        {
            var exportPart = new ExportPart();
            
            var skeletalMesh = part.Get<USkeletalMesh?>("SkeletalMesh");
            if (skeletalMesh is null) continue;
            
            if (!skeletalMesh.TryConvert(out var convertedMesh)) continue;
            if (convertedMesh.LODs.Count <= 0) continue;
            
            exportPart.MeshPath = skeletalMesh.GetPathName();
            Save(skeletalMesh);
            
            exportPart.Part = part.GetOrDefault<EFortCustomPartType>("CharacterPartType").ToString();

            var sections = convertedMesh.LODs[0].Sections.Value;
            for (var idx = 0; idx < sections.Length; idx++)
            {
                var section = sections[idx];
                if (section.Material is null) continue;
                
                
                if (!section.Material.TryLoad(out var material)) continue;
                
                var exportMaterial = new ExportMaterial
                {
                    MaterialName = material.Name,
                    SlotIndex = idx
                };

                if (material is UMaterialInstanceConstant materialInstance)
                {
                    var (textures, scalars, vectors) = MaterialParameters(materialInstance);
                    exportMaterial.Textures = textures;
                    exportMaterial.Scalars = scalars;
                    exportMaterial.Vectors = vectors;
                }
                
                exportPart.Materials.Add(exportMaterial);
                
            }
            
            if (part.TryGetValue(out FStructFallback[] materialOverrides, "MaterialOverrides"))
            {
                foreach (var materialOverride in materialOverrides)
                {
                    var overrideMaterial = materialOverride.Get<FSoftObjectPath>("OverrideMaterial");
                    if (!overrideMaterial.TryLoad(out var material)) continue;
                
                    var exportMaterial = new ExportMaterial
                    {
                        MaterialName = material.Name,
                        SlotIndex = materialOverride.Get<int>("MaterialOverrideIndex")
                    };

                    if (material is UMaterialInstanceConstant materialInstance)
                    {
                        var (textures, scalars, vectors) = MaterialParameters(materialInstance);
                        exportMaterial.Textures = textures;
                        exportMaterial.Scalars = scalars;
                        exportMaterial.Vectors = vectors;
                    }

                    for (var idx = 0; idx < exportPart.Materials.Count; idx++)
                    {
                        if (exportPart.Materials[exportMaterial.SlotIndex].MaterialName ==
                            exportPart.Materials[idx].MaterialName)
                        {
                            exportPart.OverrideMaterials.Add(exportMaterial with { SlotIndex = idx });
                        }
                    }
                }
            }
            exportParts.Add(exportPart);
        }
    }

    public static (List<TextureParameter>, List<ScalarParameter>, List<VectorParameter>) MaterialParameters(UMaterialInstanceConstant materialInstance)
    {
        var textures = new List<TextureParameter>();
        foreach (var parameter in materialInstance.TextureParameterValues)
        {
            if (!parameter.ParameterValue.TryLoad(out UTexture2D texture)) continue;
            textures.Add(new TextureParameter(parameter.ParameterInfo.Name.PlainText, texture.GetPathName()));
            Save(texture);
        }
        
        var scalars = new List<ScalarParameter>();
        foreach (var parameter in materialInstance.ScalarParameterValues)
        {
            scalars.Add(new ScalarParameter(parameter.ParameterInfo.Name.PlainText, parameter.ParameterValue));
        }
        
        var vectors = new List<VectorParameter>();
        foreach (var parameter in materialInstance.VectorParameterValues)
        {
            if (parameter.ParameterValue is null) continue;
            vectors.Add(new VectorParameter(parameter.ParameterInfo.Name.PlainText, parameter.ParameterValue.Value));
        }

        if (materialInstance.Parent is UMaterialInstanceConstant { Parent: UMaterialInstanceConstant } materialParent)
        {
            var (parentTextures, parentScalars, parentVectors) = MaterialParameters(materialParent);
            foreach (var parentTexture in parentTextures)
            {
                if (textures.Any(x => x.Name.Equals(parentTexture.Name))) continue;
                textures.Add(parentTexture);
            }
            
            foreach (var parentScalar in parentScalars)
            {
                if (scalars.Any(x => x.Name.Equals(parentScalar.Name))) continue;
                scalars.Add(parentScalar);
            }
            
            foreach (var parentVector in parentVectors)
            {
                if (vectors.Any(x => x.Name.Equals(parentVector.Name))) continue;
                vectors.Add(parentVector);
            }
        }
        return (textures, scalars, vectors);
    }
    
    public static readonly List<Task> RunningExporters = new();
    private static readonly ExporterOptions ExportOptions = new()
    {
        Platform = ETexturePlatform.DesktopMobile,
        LodFormat = ELodFormat.AllLods,
        MeshFormat = EMeshFormat.ActorX,
        TextureFormat = ETextureFormat.Png,
        ExportMorphTargets = false
    };
    
    public static void Save(UObject obj)
    {
        RunningExporters.Add(Task.Run(() =>
        {
            try
            {
                switch (obj)
                {
                    case USkeletalMesh skeletalMesh:
                    {
                        var path = GetExportPath(obj, "psk", "_LOD0");
                        if (File.Exists(path)) return;
                        
                        var exporter = new MeshExporter(skeletalMesh, ExportOptions, false);
                        exporter.TryWriteToDir(App.AssetsFolder, out _);
                        break;
                    }
                    case UTexture2D texture:
                    {
                        var path = GetExportPath(obj, "png");
                        if (File.Exists(path)) return;
                        Directory.CreateDirectory(path.Replace('\\', '/').SubstringBeforeLast('/'));
                        
                        using var bitmap = texture.Decode(texture.GetFirstMip());
                        using var data = bitmap?.Encode(SKEncodedImageFormat.Png, 100);

                        if (data is null) return;
                        File.WriteAllBytes(path, data.ToArray());
                        break;
                    }
                }
            }
            catch (IOException) { }
        }));
    }

    private static string GetExportPath(UObject obj, string ext, string extra = "")
    {
        var path = obj.Owner.Name;
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var finalPath = Path.Combine(App.AssetsFolder.FullName, path) + $"{extra}.{ext.ToLower()}";
        return finalPath;
    }
}