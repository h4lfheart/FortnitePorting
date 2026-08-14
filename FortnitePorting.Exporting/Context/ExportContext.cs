using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Dto;
using CUE4Parse_Conversion.Formats.Animations;
using CUE4Parse_Conversion.Formats.Meshes;
using CUE4Parse_Conversion.Formats.PoseAsset;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.PoseAsset;
using CUE4Parse_Conversion.Textures;
using CUE4Parse_Conversion.Writers.UEFormat.Enums;
using CUE4Parse.FileProvider.Vfs;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Component.SplineMesh;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.Utils;
using FFMpegCore;
using FortnitePorting.CUE4Parse.Extensions;
using FortnitePorting.Exporting.Extensions;
using FortnitePorting.Exporting.Models;
using Serilog;
using Image = System.Drawing.Image;

namespace FortnitePorting.Exporting.Context;

public partial class ExportContext
{
    public List<Task> ExportTasks = [];

    public readonly ExportDataMeta Meta;
    public CancellationToken CancellationToken => Meta.CancellationToken;
    private readonly ExportOptions FileExportOptions;

    private AbstractVfsFileProvider FileProvider => Meta.Provider.Provider;

    public ExportContext(ExportDataMeta metaData)
    {
        Meta = metaData;
        FileExportOptions = Meta.Settings.CreateExportOptions();
    }

    public async Task<string> ExportAsync(UObject asset, bool returnRealPath = false, bool synchronousExport = false, bool embeddedAsset = false, bool isNanite = false)
    {
        var extension = asset switch
        {
            USkeletalMesh or UStaticMesh or USkeleton or USplineMeshComponent or ALandscapeProxy => MeshExtension(Meta.Settings.MeshFormat),
            UAnimSequenceBase => AnimExtension(Meta.Settings.MeshFormat),
            UPoseAsset => "uepose",
            UTexture => Meta.Settings.ImageFormat switch
            {
                EImageFormat.PNG => "png",
                EImageFormat.TGA => "tga"
            },
            USoundWave => Meta.Settings.SoundFormat switch
            {
                ESoundFormat.WAV => "wav",
                ESoundFormat.MP3 => "mp3",
                ESoundFormat.OGG => "ogg",
                ESoundFormat.FLAC => "flac",
            },
            UFontFace => "ttf"
        };

        var path = GetExportPath(asset, extension, embeddedAsset, excludeGamePath: Meta.CustomPath is not null);
        
        var returnValue = returnRealPath ? path : (embeddedAsset ? $"{asset.Owner.Name}/{asset.Name}.{asset.Name}" : asset.GetPathName());

        if (isNanite)
        {
            if (returnRealPath)
            {
                returnValue = ApplyNameSuffix(path, "_Nanite");
            }
            else
            {
                var naniteName = returnValue.SubstringAfterLast(".") + "_Nanite";
                returnValue = $"{returnValue.SubstringBeforeLast("/")}/{naniteName}.{naniteName}";
            }
        }

        if (asset is USplineMeshComponent splineComponent)
        {
            var assetName = $"{asset.Name}-{splineComponent.GetMeshId().AsSpan(0, 6)}";
            if (isNanite) assetName += "_Nanite";
            returnValue = $"{asset.Owner.Name}/{assetName}.{assetName}";
        }
        
        var shouldExport = asset switch
        {
            UTexture texture => IsTextureHigherResolutionThanExisting(texture, path),
            UAnimSequence animSequence when animSequence.IsValidAdditive() => true,
            ALandscapeProxy => true,
            UStaticMesh or USkeletalMesh when NeedsNaniteFile(asset, path) => true,
            _ when IsOutdatedUEFormat(path) => true,
            _ => !File.Exists(path)
        };

        if (!shouldExport) return returnValue;

        var exportTask = new Task(() =>
        {
            try
            {
                Log.Information("Exporting {ExportType}: {Path}", asset.ExportType, path);
                Export(asset, path);
            }
            catch (IOException e)
            {
                if ((e.HResult & 0x0000FFFF) == 32) return; // locked files, move on, it's being exported anyways

                Log.Warning("Failed to Export {ExportType}: {Name}", asset.ExportType, asset.Name);
                Log.Warning(e.ToString());
            }
            catch (Exception e)
            {
                Log.Warning("Failed to Export {ExportType}: {Name}", asset.ExportType, asset.Name);
                Log.Warning(e.ToString());
            } 
        });
        
        ExportTasks.Add(exportTask);

        if (synchronousExport)
            exportTask.RunSynchronously();
        else
            exportTask.Start();

        return returnValue;
    }
    
    public string Export(UObject asset, bool returnRealPath = false, bool synchronousExport = false, bool embeddedAsset = false, bool isNanite = false)
    {
        return ExportAsync(asset, returnRealPath, synchronousExport, embeddedAsset, isNanite).GetAwaiter().GetResult();
    }

    private void Export(UObject asset, string path)
    {
        switch (asset)
        {
            case USkeletalMesh skeletalMesh:
            {
                if (FileExportOptions.ExportMorphTargets)
                    skeletalMesh.PopulateMorphTargetVerticesData();

                using var dto = new SkeletalMeshDto(skeletalMesh, FileExportOptions.MeshQuality, FileExportOptions.NaniteMeshFormat);
                WriteExportFiles(path, CreateMeshFormat().BuildSkeletalMesh(skeletalMesh.Name, skeletalMesh.GetPathName(), FileExportOptions, dto));
                break;
            }
            case UStaticMesh staticMesh:
            {
                using var dto = new StaticMeshDto(staticMesh, FileExportOptions.MeshQuality, FileExportOptions.NaniteMeshFormat);
                WriteExportFiles(path, CreateMeshFormat().BuildStaticMesh(staticMesh.Name, staticMesh.GetPathName(), FileExportOptions, dto));
                break;
            }
            case USplineMeshComponent splineMesh:
            {
                using var dto = new StaticMeshDto(splineMesh, FileExportOptions.MeshQuality);
                WriteExportFiles(path, CreateMeshFormat().BuildStaticMesh(splineMesh.Name, splineMesh.GetPathName(), FileExportOptions, dto));
                break;
            }
            case USkeleton skeleton:
            {
                using var dto = new SkeletonDto(skeleton);
                WriteExportFiles(path, CreateMeshFormat().BuildSkeleton(skeleton.Name, skeleton.GetPathName(), FileExportOptions, dto));
                break;
            }
            case UAnimStreamable animStreamable:
            {
                if (CreateAnimFormat() is not UEFormatAnimFormat ueAnimFormat)
                    throw new NotSupportedException($"Anim streamable export is not supported for {FileExportOptions.MeshFormat}.");

                WriteExportFiles(path, ueAnimFormat.BuildAnimStreamable(animStreamable.Name, animStreamable.GetPathName(), FileExportOptions, animStreamable));
                break;
            }
            case UPoseAsset poseAsset:
            {
                if (FileExportOptions.MeshFormat is not EMeshFormat.UEFormat)
                    throw new NotSupportedException($"Pose asset export is not supported for {FileExportOptions.MeshFormat}.");

                if (!poseAsset.TryConvert(out var convertedPoseAsset))
                    throw new Exception($"Failed to convert pose asset '{poseAsset.Name}'");

                var poseFile = new UEFormatPoseFormat().Build(poseAsset.Name, poseAsset.GetPathName(), FileExportOptions, convertedPoseAsset);
                WriteExportFiles(path, [poseFile]);
                break;
            }
            case UAnimationAsset animation:
            {
                WriteExportFiles(path, CreateAnimFormat().Build(animation.Name, animation.GetPathName(), FileExportOptions, animation.ConvertAnims()));
                break;
            }
            case UTexture2DArray textureArray:
            {
                var textures = textureArray.DecodeTextureArray();
                if (textures == null) break;
                
                for (var layerIndex = 0; layerIndex < textures.Length; layerIndex++)
                {
                    var textureBitmap = textures[layerIndex];
                    var texturePath = path.Replace(".png", $"_{layerIndex}.png");
                    ExportBitmap(textureBitmap, texturePath);
                }
                
                break;
            }
            case UTexture texture:
            {
                var textureBitmap = texture.Decode();
                if (texture is UTextureCube)
                {
                    textureBitmap = textureBitmap?.ToPanorama();
                    
                    using var fileStream = File.OpenWrite(Path.ChangeExtension(path, "hdr")); 
                    fileStream.Write(textureBitmap!.ToHdrBitmap());
                    break;
                }
                ExportBitmap(textureBitmap, path);

                break;
            }
            case USoundWave soundWave:
            {
                var wavPath = Path.ChangeExtension(path, "wav");
                if (!SoundExtensions.TrySaveSoundToPath(soundWave, wavPath, Meta.Provider.BinkaDecoderFile, Meta.Provider.RadaDecoderFile, Meta.Provider.VgmStreamFile))
                {
                    throw new Exception($"Failed to export sound '{soundWave.Name}' at {path}");
                }

                if (Meta.Settings.SoundFormat is not ESoundFormat.WAV)
                {
                    var extension = Path.GetExtension(path)[1..];
                    
                    FFMpegArguments.FromFileInput(wavPath)
                        .OutputToFile(path, true, options => options.ForceFormat(extension))
                        .ProcessSynchronously();
                        
                    File.Delete(wavPath);
                }

                
                break;
            }
            case ALandscapeProxy landscapeProxy:
            {
                using var dto = new LandscapeMeshDto(landscapeProxy, ELandscapeFlags.Mesh);
                WriteExportFiles(path, CreateMeshFormat().BuildStaticMesh(landscapeProxy.Name, landscapeProxy.GetPathName(), FileExportOptions, dto));
                break;
            }
            case UFontFace fontFace:
            {
                if (!FileProvider.TrySavePackage(fontFace.GetPathName().SubstringBeforeLast(".") + ".ufont",
                        out var assets) || assets.Count == 0) break;

                var fontData = assets.First().Value;
                File.WriteAllBytes(path, fontData);
                break;
            }
        }
    }

    private IMeshExportFormat CreateMeshFormat() => FileExportOptions.MeshFormat switch
    {
        EMeshFormat.ActorX => new ActorXMeshFormat(),
        EMeshFormat.Gltf2 => new GltfMeshFormat(),
        EMeshFormat.USD => new UsdMeshFormat(),
        _ => new UEFormatMeshFormat(Meta.Settings.ExportNanite)
    };

    private IAnimExportFormat CreateAnimFormat() => FileExportOptions.MeshFormat switch
    {
        EMeshFormat.ActorX => new ActorXAnimFormat(),
        EMeshFormat.USD => new UsdAnimFormat(),
        _ => new UEFormatAnimFormat()
    };

    private static string MeshExtension(EMeshFormat format) => format switch
    {
        EMeshFormat.ActorX => "psk",
        EMeshFormat.Gltf2 => "glb",
        EMeshFormat.USD => "usda",
        _ => "uemodel"
    };

    private static string AnimExtension(EMeshFormat format) => format switch
    {
        EMeshFormat.ActorX => "psa",
        EMeshFormat.USD => "usda",
        _ => "ueanim"
    };

    private static void WriteExportFiles(string basePath, IReadOnlyList<ExportFile> files)
    {
        foreach (var file in files)
        {
            var dest = ApplyNameSuffix(basePath, file.NameSuffix, file.Extension);
            var destDir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(destDir))
                Directory.CreateDirectory(destDir);
            File.WriteAllBytes(dest, file.Data);
        }
    }

    private string ExportMeshFiles(UObject asset, IReadOnlyList<ExportFile> files, bool embeddedAsset = false)
    {
        var path = GetExportPath(asset, MeshExtension(Meta.Settings.MeshFormat), embeddedAsset, excludeGamePath: Meta.CustomPath is not null);
        var hasNaniteFile = files.Any(file => file.NameSuffix == "_Nanite");
        var nanitePath = hasNaniteFile ? ApplyNameSuffix(path, "_Nanite") : null;
        var shouldExport = !File.Exists(path) || IsOutdatedUEFormat(path)
            || (nanitePath is not null && (!File.Exists(nanitePath) || IsOutdatedUEFormat(nanitePath)));

        if (shouldExport)
        {
            var exportTask = new Task(() =>
            {
                try
                {
                    Log.Information("Exporting {ExportType}: {Path}", asset.ExportType, path);
                    WriteExportFiles(path, files);
                }
                catch (IOException e)
                {
                    if ((e.HResult & 0x0000FFFF) == 32) return;

                    Log.Warning("Failed to Export {ExportType}: {Name}", asset.ExportType, asset.Name);
                    Log.Warning(e.ToString());
                }
                catch (Exception e)
                {
                    Log.Warning("Failed to Export {ExportType}: {Name}", asset.ExportType, asset.Name);
                    Log.Warning(e.ToString());
                }
            });

            ExportTasks.Add(exportTask);
            exportTask.Start();
        }

        return PluginObjectPath(asset, embeddedAsset, hasNaniteFile);
    }

    private static string PluginObjectPath(UObject asset, bool embeddedAsset, bool isNanite)
    {
        if (asset is USplineMeshComponent splineComponent)
        {
            var assetName = $"{asset.Name}-{splineComponent.GetMeshId().AsSpan(0, 6)}";
            if (isNanite) assetName += "_Nanite";
            return $"{asset.Owner.Name}/{assetName}.{assetName}";
        }

        var returnValue = embeddedAsset
            ? $"{asset.Owner.Name}/{asset.Name}.{asset.Name}"
            : asset.GetPathName();

        if (!isNanite) return returnValue;

        var naniteName = returnValue.SubstringAfterLast(".") + "_Nanite";
        return $"{returnValue.SubstringBeforeLast("/")}/{naniteName}.{naniteName}";
    }

    private bool NeedsNaniteFile(UObject asset, string path)
    {
        var nanitePath = ApplyNameSuffix(path, "_Nanite");
        if (!Meta.Settings.ExportNanite || (File.Exists(nanitePath) && !IsOutdatedUEFormat(nanitePath)))
            return false;

        return asset switch
        {
            UStaticMesh staticMesh => staticMesh.RenderData?.NaniteResources is { PageStreamingStates.Length: > 0 },
            USkeletalMesh skeletalMesh => skeletalMesh.NaniteResources is { PageStreamingStates.Length: > 0 },
            _ => false
        };
    }

    private static string ApplyNameSuffix(string path, string? suffix, string? extension = null)
    {
        var directory = Path.GetDirectoryName(path) ?? string.Empty;
        var stem = Path.GetFileNameWithoutExtension(path);
        var ext = (extension ?? Path.GetExtension(path).TrimStart('.')).ToLowerInvariant();
        if (!string.IsNullOrEmpty(suffix))
            stem += suffix;
        return Path.Combine(directory, $"{stem}.{ext}");
    }

    private static bool IsOutdatedUEFormat(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            if (Path.GetExtension(path).ToLowerInvariant() is not (".uemodel" or ".ueanim" or ".uepose"))
                return false;

            using var file = File.OpenRead(path);
            using var reader = new BinaryReader(file);

            Span<byte> magic = stackalloc byte[8];
            if (reader.Read(magic) != 8 || !magic.SequenceEqual("UEFORMAT"u8))
                return true;

            var identifierLength = reader.ReadInt32();
            if (identifierLength is < 0 or > 255)
                return true;

            reader.ReadBytes(identifierLength);

            var version = (EUEFormatVersion) reader.ReadByte();
            return version < EUEFormatVersion.LatestVersion;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private bool IsTextureHigherResolutionThanExisting(UTexture texture, string path)
    {
        try
        {
            if (!File.Exists(path)) return true;
            
            using var file = File.OpenRead(path);
            using var image = Image.FromStream(file, useEmbeddedColorManagement: false, validateImageData: false);
            
            var mip = texture.GetFirstMip();
            if (mip is null) return true;
            
            return mip.SizeX > image.PhysicalDimension.Width && mip?.SizeY > image.PhysicalDimension.Height;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private void ExportBitmap(CTexture? bitmap, string path)
    {
        using var fileStream = File.OpenWrite(path); 
                
        var format = Meta.Settings.ImageFormat switch
        {
            EImageFormat.PNG => ETextureFormat.Png,
            EImageFormat.TGA => ETextureFormat.Tga,
        };
        
        fileStream.Write(bitmap?.Encode(format, false, out _));
    }
    
    public string GetExportPath(UObject obj, string ext, bool embeddedAsset = false, bool excludeGamePath = false)
    {
        string path;
        if (excludeGamePath || obj.Owner is null)
        {
            path = obj.Name;
        }
        else
        {
            path = embeddedAsset ? $"{obj.Owner.Name}/{obj.Name}" : obj.Owner?.Name ?? string.Empty;
        }

        return BuildExportPath(path, ext, obj);
    }
    
    public string BuildExportPath(string path, string ext, UObject? obj = null)
    {
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var directory = Path.Combine(Meta.CustomPath ?? Meta.AssetsRoot, path);
        Directory.CreateDirectory(directory.SubstringBeforeLast("/"));

        if (obj is USplineMeshComponent splineComponent)
            directory += string.Concat("-", splineComponent.GetMeshId().AsSpan(0, 6));
        
        var finalPath = $"{directory}.{ext.ToLower()}";
        return finalPath;
    }
}
