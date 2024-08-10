using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Readers;

namespace FortnitePorting.Models.Fortnite;

public class UFortGameFeatureData : UObject
{
    public FVkExperienceData? ExperienceData;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);
        
        ExperienceData = GetOrDefault<FVkExperienceData>(nameof(ExperienceData));
    }
}
