using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine.Animation;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Exporting.Models.Files.Meta;

namespace FortnitePorting.Exporting.Types;

public class PoseAssetExport : BaseExport
{
    public string PoseAsset;

    public PoseAssetExport(string name, UObject asset, EExportType exportType, ExportDataMeta metaData, IExportFileMeta? fileMeta) : base(name, exportType, metaData)
    {
        if (asset is not UPoseAsset poseAsset) return;
        if (metaData.ExportLocation.IsFolder) return;

        PoseAsset = Exporter.Export(poseAsset);
    }
}
