using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using FortnitePorting.Exporting.Custom;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Exporting.Models.Files;
using FortnitePorting.Exporting.Models.Files.Meta;
using FortnitePorting.Exporting.Styles;
using FortnitePorting.Exporting.Types;
using FortnitePorting.Models;

namespace FortnitePorting.Exporting;

public class ExportSession
{
    public ExportDataMeta Meta { get; }

    public Func<string, Stream>? OpenCustomAssetResource { get; }

    public ExportSession(ExportDataMeta meta, Func<string, Stream>? openCustomAssetResource = null)
    {
        Meta = meta;
        OpenCustomAssetResource = openCustomAssetResource;
    }

    public async Task<ExportData> RunAsync(IEnumerable<BaseExport> exports)
    {
        var list = new List<BaseExport>();
        foreach (var export in exports)
        {
            list.Add(export);
            if (Meta.CancellationToken.IsCancellationRequested) break;
        }

        foreach (var export in list)
            await export.WaitForExports();

        return new ExportData
        {
            MetaData = Meta,
            Exports = list.ToArray()
        };
    }

    public async Task<ExportData> RunAsync(Func<IEnumerable<BaseExport>> exportFunction)
        => await RunAsync(exportFunction());

    public BaseExport CreateExport(
        string displayName,
        UObject asset,
        EExportType exportType,
        ExportStyleBase[] styles,
        IExportFileMeta? fileMeta = null)
    {
        var primitiveType = exportType.PrimitiveType;
        return primitiveType switch
        {
            EPrimitiveExportType.Mesh => new MeshExport(displayName, asset, styles, exportType, Meta, fileMeta),
            EPrimitiveExportType.Texture => new TextureExport(displayName, asset, exportType, Meta, fileMeta),
            EPrimitiveExportType.Sound => new SoundExport(displayName, asset, exportType, Meta, fileMeta),
            EPrimitiveExportType.Animation => new AnimExport(displayName, asset, styles, exportType, Meta, fileMeta),
            EPrimitiveExportType.Font => new FontExport(displayName, asset, exportType, Meta, fileMeta),
            EPrimitiveExportType.PoseAsset => new PoseAssetExport(displayName, asset, exportType, Meta, fileMeta),
            EPrimitiveExportType.Material => new MaterialExport(displayName, asset, exportType, Meta, fileMeta),
            _ => throw new NotImplementedException($"Exporting {primitiveType} assets is not supported yet.")
        };
    }

    public MeshExport CreateCustomMeshExport(string name, MeshDefinition mesh, EExportType exportType)
    {
        if (OpenCustomAssetResource is null)
            throw new InvalidOperationException("CreateCustomMeshExport requires ExportSession.OpenCustomAssetResource.");

        return new MeshExport(name, mesh, OpenCustomAssetResource, exportType, Meta);
    }

    public BaseExport CreateTastyExport() => new TastyExport(Meta);

    public static string FixPath(string path)
    {
        var outPath = path.SubstringBeforeLast(".");
        var extension = path.SubstringAfterLast(".");
        if (extension.Equals("umap") && outPath.Contains("_Generated_"))
        {
            outPath += "." + path.SubstringBeforeLast("/_Generated").SubstringAfterLast("/");
        }

        return outPath;
    }
}
