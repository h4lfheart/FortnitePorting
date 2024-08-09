using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Fortnite;

[StructFallback]
public class FVkMapExperienceData(FStructFallback fallback)
{
    public FSoftObjectPath Map = fallback.GetOrDefault<FSoftObjectPath>(nameof(Map));
    public FSoftObjectPath[] BaseMaps = fallback.GetOrDefault<FSoftObjectPath[]>(nameof(BaseMaps));
}
