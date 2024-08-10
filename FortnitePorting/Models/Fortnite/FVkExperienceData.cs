using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Fortnite;

[StructFallback]
public class FVkExperienceData(FStructFallback fallback)
{
    public FSoftObjectPath? DefaultMap = fallback.GetOrDefault<FSoftObjectPath>(nameof(DefaultMap));
    public FSoftObjectPath? BaseMap = fallback.GetOrDefault<FSoftObjectPath>(nameof(BaseMap));
    public FVkMapExperienceData? MapData = fallback.GetOrDefault<FVkMapExperienceData>(nameof(MapData));
}
