using CUE4Parse.UE4.Assets.Exports.Animation;

namespace FortnitePorting.Exporting.Models.Files.Meta;

public class ExportAdditiveAnimFileMeta : IExportFileMeta
{
    public UAnimSequence BaseSequence { get; set; }
}