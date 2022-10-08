using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.Utils;

namespace FortnitePorting.Export;

public static class ExportHelpers
{
    public static List<Task> RunningExporters = new();
    private static ExporterOptions ExportOptions = new()
    {
        Platform = ETexturePlatform.DesktopMobile,
        LodFormat = ELodFormat.AllLods,
        MeshFormat = EMeshFormat.ActorX,
        TextureFormat = ETextureFormat.Png,
        ExportMorphTargets = false
    };
    
    public static async Task<string> ExportObjectAsync(UObject file)
    {
        var currentTask = Task.Run(() =>
        {
            try
            {
                if (file is USkeletalMesh skeletalMesh)
                {
                    var exporter = new MeshExporter(skeletalMesh, ExportOptions, false);
                    exporter.TryWriteToDir(App.AssetsFolder, out _);
                    return GetExportPath(skeletalMesh, "psk", "_LOD0");
                }
            }
            catch (IOException) { }

            return string.Empty;
        });
        
        RunningExporters.Add(currentTask);
        return currentTask.Result;
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