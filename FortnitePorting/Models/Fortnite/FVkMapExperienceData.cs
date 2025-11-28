using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Fortnite;

[StructFallback]
public class FVkMapExperienceData
{
    [UProperty] public FSoftObjectPath Map;
    [UProperty] public FSoftObjectPath[] BaseMaps;
}
