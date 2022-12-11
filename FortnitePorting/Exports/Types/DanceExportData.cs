using System;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Exports.Types;

public class DanceExportData : ExportDataBase
{
    public string Animation;
    public static async Task<DanceExportData> Create(UObject asset)
    {
        var data = new DanceExportData();
        data.Name = asset.GetOrDefault("DisplayName", new FText("Unnamed")).Text;
        data.Type = EAssetType.Dance.ToString();
        await Task.Run(() =>
        {
            var montage = asset.Get<UAnimMontage>("Animation");
            var sections = montage.Get<FStructFallback[]>("CompositeSections");
            var targetSection = sections.First(x =>
            {
                var sectionText = x.GetOrDefault<FName>("SectionName").Text;
                return sectionText.Equals("Default") || sectionText.Equals("Loop");
            });
            
            var animation = targetSection.Get<UAnimSequence>("LinkedSequence");
            ExportHelpers.Save(animation);
            data.Animation = animation.GetPathName();
        });

        await Task.WhenAll(ExportHelpers.Tasks);
        return data;
    }
}