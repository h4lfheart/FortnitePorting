using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Readers;

namespace FortnitePorting.Models.Fortnite;

public class FortAnimNotifyState_EmoteSound : UObject
{
    [UProperty] public USoundCue? EmoteSound1P;
    [UProperty] public USoundCue? EmoteSound3P;
}