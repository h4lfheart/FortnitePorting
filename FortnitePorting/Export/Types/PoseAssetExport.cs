using System;
using System.Collections.Generic;
using System.Linq;
using CUE4Parse.GameTypes.FN.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Component.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Engine.Font;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Engine.Animation;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using DynamicData;
using FortnitePorting.Export.Models;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.CUE4Parse;
using FortnitePorting.Models.Unreal.Landscape;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using Serilog;

namespace FortnitePorting.Export.Types;

public class PoseAssetExport : BaseExport
{
    public List<PoseData> PoseData = [];
    public string[] CurveTrackNames = [];

    public PoseAssetExport(string name, UObject asset, BaseStyleData[] styles, EExportType exportType, ExportDataMeta metaData) : base(name, asset, styles, exportType, metaData)
    {
        if (asset is not UPoseAsset poseAsset) return;
        if (metaData.ExportLocation.IsFolder())
        {
            AppWM.Message("Pose Asset Export", "Pose Assets cannot be exported to a folder.");
            return;
        }

        var meta = new ExportPoseDataMeta();
        Exporter.PoseAsset(poseAsset, meta);

        PoseData = meta.PoseData;
        CurveTrackNames = meta.CurveTrackNames;
    }
    
}