using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine.Animation;
using FortnitePorting.Exporting.Models;

namespace FortnitePorting.Exporting.Types;

public class PoseAssetExport : BaseExport
{
    public string PoseAsset;
    
    public PoseAssetExport(string name, UObject asset, EExportType exportType, ExportDataMeta metaData) : base(name, exportType, metaData)
    {
        if (asset is not UPoseAsset poseAsset) return;
        if (metaData.ExportLocation.IsFolder)
        {
            Info.Message("Pose Asset Export", "Pose Assets cannot be exported to a folder.");
            return;
        }

        PoseAsset = Exporter.Export(poseAsset);
    }
    
}