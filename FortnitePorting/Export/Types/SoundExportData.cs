using System.IO;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;

namespace FortnitePorting.Export.Types;

public class SoundExportData : ExportDataBase
{
    public SoundExportData(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportTargetType exportType) : base(name, asset, styles, type, EExportType.Animation, exportType)
    {
        if (asset is not USoundWave sound) return;
        
        var exportPath = Exporter.Export(sound, true);
        Launch(Path.GetDirectoryName(exportPath)!);
    }

}