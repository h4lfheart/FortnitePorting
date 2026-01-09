using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Models.Fortnite;

public class UFortItemSeriesDefinition : UObject
{
    [UProperty] public FText DisplayName;
    [UProperty] public FRarityCollection Colors;
    [UProperty] public FSoftObjectPath BackgroundTexture;
}
