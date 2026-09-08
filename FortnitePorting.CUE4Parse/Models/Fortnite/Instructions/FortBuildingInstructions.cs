using CUE4Parse.GameTypes.FN.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Utils;

namespace FortnitePorting.CUE4Parse.Models.Fortnite.Instructions;

public class FortBuildingInstructions : UObject
{
    [UProperty] public FFortBuildingInstruction[]? Instructions;
}

[StructFallback]
public class FFortBuildingInstruction
{
    [UProperty] public FFortActorRecord? ActorRecord;
}
