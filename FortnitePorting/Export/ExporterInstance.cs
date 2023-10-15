using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.ViewModels;
using Serilog;

namespace FortnitePorting.Export;

public class ExporterInstance
{
    public readonly List<Task> Tasks = new();
    private readonly ExportOptionsBase AppExportOptions;
    private readonly ExporterOptions FileExportOptions;

    public ExporterInstance(ExportOptionsBase exportOptions)
    {
        AppExportOptions = exportOptions;
        FileExportOptions = AppExportOptions.CreateExportOptions();
    }

    public ExportTypes.Part? CharacterPart(UObject part)
    {
        var skeletalMesh = part.GetOrDefault<USkeletalMesh?>("SkeletalMesh");
        if (skeletalMesh is null) return null;
        
        if (!skeletalMesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;
        
        var exportPart = new ExportTypes.Part();
        exportPart.Path = Export(skeletalMesh);
        exportPart.NumLods = convertedMesh.LODs.Count;
        exportPart.Type = part.GetOrDefault<EFortCustomPartType>("CharacterPartType").ToString();
        exportPart.AttachToSocket = part.GetOrDefault("bAttachToSocket", false);

        if (part.TryGetValue(out UObject additionalData, "AdditionalData"))
        {
            exportPart.Socket = additionalData.GetOrDefault<FName?>("AttachSocketName")?.Text;
        }

        return exportPart;
    }
    
    public ExportTypes.Mesh? Mesh(USkeletalMesh mesh)
    {
        if (!mesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;
        
        var exportPart = new ExportTypes.Mesh();
        exportPart.Path = Export(mesh);
        exportPart.NumLods = convertedMesh.LODs.Count;
        return exportPart;
    }
    
    public ExportTypes.Mesh? Mesh(UStaticMesh mesh)
    {
        if (!mesh.TryConvert(out var convertedMesh)) return null;
        if (convertedMesh.LODs.Count <= 0) return null;
        
        var exportPart = new ExportTypes.Mesh();
        exportPart.Path = Export(mesh);
        exportPart.NumLods = convertedMesh.LODs.Count;
        return exportPart;
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