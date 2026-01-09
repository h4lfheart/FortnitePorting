using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Engine.Animation;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Models.Assets;

namespace FortnitePorting.Exporting.Types;

public class PoseAssetExport : BaseExport
{
    public string PoseAsset;
    
    public PoseAssetExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
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