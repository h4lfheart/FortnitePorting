using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using FortnitePorting.Exporting.Models;

namespace FortnitePorting.Exporting.Types;

public class TastyExport : BaseExport
{
    public ExportMesh? MasterSkeletalMesh;

    public TastyExport(ExportDataMeta metaData) : base("Tasty Rig", EExportType.TastyRig, metaData)
    {
        MasterSkeletalMesh = Exporter.Mesh(metaData.Provider.Provider.SafeLoadPackageObject<USkeletalMesh>("/FortniteGame/Content/Characters/Player/Male/Medium/Base/SK_M_MALE_Base_Skeleton"));
    }
}
