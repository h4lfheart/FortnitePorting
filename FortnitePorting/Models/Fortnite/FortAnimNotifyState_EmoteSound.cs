using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;

namespace FortnitePorting.Models.Fortnite;

public class FortAnimNotifyState_EmoteSound : UObject
{
    [UProperty] public USoundCue? EmoteSound1P;
    [UProperty] public USoundCue? EmoteSound3P;
    
    [UProperty("EmoteSound1P")] public UMetaSoundSource? MetaEmoteSound1P;
    [UProperty("EmoteSound3P")] public UMetaSoundSource? MetaEmoteSound3P;
}