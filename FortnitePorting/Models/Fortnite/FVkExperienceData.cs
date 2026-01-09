using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Fortnite;

[StructFallback]
public class FVkExperienceData
{
    [UProperty] public FSoftObjectPath? DefaultMap;
    [UProperty] public FSoftObjectPath? BaseMap;
    [UProperty] public FVkMapExperienceData? MapData;
}
