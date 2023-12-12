using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Objects;
using FortnitePorting.Extensions;

namespace FortnitePorting.Export.Types;

public class AnimExportData : ExportDataBase
{
    public List<ExportAnimSection> Sections = new();
    
    public AnimExportData(string name, UObject asset, FStructFallback[] styles, EAssetType type, EExportTargetType exportType) : base(name, asset, styles, type, EExportType.Animation, exportType)
    {
        switch (type)
        {
            case EAssetType.Animation:
            {
                switch (asset)
                {
                    case UAnimSequence animSequence:
                    {
                        Sections.AddIfNotNull(Exporter.AnimSequence(animSequence));
                        break;
                    }
                    case UAnimMontage animMontage:
                    {
                        break;
                    }
                }
                break;
            }
            case EAssetType.Emote:
            {
                break;
            }
        }
    }
}