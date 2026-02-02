using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Exports.Animation.CurveExpression;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.Exporting.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Exporting.Types;

public class TastyExport : BaseExport
{
    public ExportMesh? MasterSkeletalMesh;
    
    public TastyExport(ExportDataMeta metaData) : base("Tasty Rig", EExportType.TastyRig, metaData)
    {
        MasterSkeletalMesh = Exporter.Mesh(UEParse.Provider.SafeLoadPackageObject<USkeletalMesh>("/FortniteGame/Content/Characters/Player/Male/Medium/Base/SK_M_MALE_Base_Skeleton"));
    }
    
}