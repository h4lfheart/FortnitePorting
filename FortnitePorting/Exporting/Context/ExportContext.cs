using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.UEFormat;
using CUE4Parse_Conversion.PoseAsset;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Actor;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Writers;
using CUE4Parse.Utils;
using FFMpegCore;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Services;
using Serilog;
using Image = System.Drawing.Image;

namespace FortnitePorting.Exporting.Context;

public partial class ExportContext
{
    public List<Task> ExportTasks = [];

    public readonly ExportDataMeta Meta;
    private readonly ExporterOptions FileExportOptions;

    public ExportContext(ExportDataMeta metaData)
    {
        Meta = metaData;
        FileExportOptions = Meta.Settings.CreateExportOptions();
    }

    public async Task<string> ExportAsync(UObject asset, bool returnRealPath = false, bool synchronousExport = false, bool embeddedAsset = false)
    {
        var extension = asset switch
        {
            USkeletalMesh or UStaticMesh or USkeleton => Meta.Settings.MeshFormat switch
            {
                EMeshFormat.UEFormat => "uemodel",
                EMeshFormat.ActorX => "psk",
                EMeshFormat.Gltf2 => "glb",
                EMeshFormat.OBJ => "obj",
            },
            UAnimSequence => Meta.Settings.AnimFormat switch
            {
                EAnimFormat.UEFormat => "ueanim",
                EAnimFormat.ActorX => "psa"
            },
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
            ALandscapeProxy => "uemodel",
            UFontFace => "ttf"
        };

        var path = GetExportPath(asset, extension, embeddedAsset, excludeGamePath: Meta.CustomPath is not null);
        
        var returnValue = returnRealPath ? path : (embeddedAsset ? $"{asset.Owner.Name}/{asset.Name}.{asset.Name}" : asset.GetPathName());

        var shouldExport = asset switch
        {
            UTexture texture => IsTextureHigherResolutionThanExisting(texture, path),
            ALandscapeProxy => true,
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
            exportTask.RunAsynchronously();

        return returnValue;
    }
    
    public string Export(UObject asset, bool returnRealPath = false, bool synchronousExport = false, bool embeddedAsset = false)
    {
        return ExportAsync(asset, returnRealPath, synchronousExport, embeddedAsset).GetAwaiter().GetResult();
    }

    private void Export(UObject asset, string path)
    {
        switch (asset)
        {
            case USkeletalMesh skeletalMesh:
            {
                var exporter = new MeshExporter(skeletalMesh, FileExportOptions);
                foreach (var mesh in exporter.MeshLods)
                {
                    File.WriteAllBytes(path, mesh.FileData);
                }
                break;
            }
            case UStaticMesh staticMesh:
            {
                var exporter = new MeshExporter(staticMesh, FileExportOptions);
                foreach (var mesh in exporter.MeshLods)
                {
                    File.WriteAllBytes(path, mesh.FileData);
                }
                break;
            }
            case USkeleton skeleton:
            {
                var exporter = new MeshExporter(skeleton, FileExportOptions);
                foreach (var skel in exporter.MeshLods)
                {
                    File.WriteAllBytes(path, skel.FileData);
                }
                break;
            }
            case UAnimSequence animSequence:
            {
                var exporter = new AnimExporter(animSequence, FileExportOptions);
                foreach (var sequence in exporter.AnimSequences)
                {
                    File.WriteAllBytes(path, sequence.FileData);
                }
                break;
            }
            case UPoseAsset poseAsset:
            {
                var exporter = new PoseAssetExporter(poseAsset, FileExportOptions);
                File.WriteAllBytes(path, exporter.PoseAsset.FileData);
                break;
            }
            case UTexture texture:
            {
                if (texture is UTexture2DArray && texture.GetFirstMip() is { } mip)
                {
                    for (var layerIndex = 0; layerIndex < mip.SizeZ; layerIndex++)
                    {
                        var textureBitmap = texture.Decode(mip, zLayer: layerIndex);
                        var texturePath = path.Replace(".png", $"_{layerIndex}.png");
                        ExportBitmap(textureBitmap, texturePath);
                    }
                }
                else
                {
                    var textureBitmap = texture.Decode();
                    if (texture is UTextureCube)
                    {
                        textureBitmap = textureBitmap?.ToPanorama();
                        
                        using var fileStream = File.OpenWrite(Path.ChangeExtension(path, "hdr")); 
                        fileStream.Write(textureBitmap!.ToHdrBitmap());
                    }
                    else
                    {
                        ExportBitmap(textureBitmap, path);
                    }
                }

                break;
            }
            case USoundWave soundWave:
            {
                var wavPath = Path.ChangeExtension(path, "wav");
                if (!SoundExtensions.TrySaveSoundToPath(soundWave, wavPath))
                {
                    throw new Exception($"Failed to export sound '{soundWave.Name}' at {path}");
                }

                if (Meta.Settings.SoundFormat is not ESoundFormat.WAV)
                {
                    var extension = Path.GetExtension(path)[1..];
                    
                    // convert to format
                    FFMpegArguments.FromFileInput(wavPath)
                        .OutputToFile(path, true, options => options.ForceFormat(extension))
                        .ProcessSynchronously();
                        
                    File.Delete(wavPath); // delete old wav
                }

                
                break;
            }
            case ALandscapeProxy landscapeProxy:
            {
                var processor = new LandscapeProcessor(landscapeProxy);
                var mesh = processor.Process();

                var archive = new FArchiveWriter();
                var model = new UEModel(landscapeProxy.Name, mesh, new FPackageIndex(), FileExportOptions);
                model.Save(archive);

                File.WriteAllBytes(path, archive.GetBuffer());
                break;
            }
            case UFontFace fontFace:
            {
                if (!UEParse.Provider.TrySavePackage(fontFace.GetPathName().SubstringBeforeLast(".") + ".ufont",
                        out var assets) || assets.Count == 0) break;

                var fontData = assets.First().Value;
                File.WriteAllBytes(path, fontData);
                break;
            }
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
        
        fileStream.Write(bitmap?.Encode(format, out _));
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
        
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var directory = Path.Combine(Meta.CustomPath ?? Meta.AssetsRoot, path);
        Directory.CreateDirectory(directory.SubstringBeforeLast("/"));

        var finalPath = $"{directory}.{ext.ToLower()}";
        return finalPath;
    }
}