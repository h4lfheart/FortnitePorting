using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Readers;

namespace FortnitePorting.Models.Fortnite;

public class FortAnimNotifyState_EmoteSound : UObject
{
    public USoundCue? EmoteSound1P;
    public USoundCue? EmoteSound3P;

    public override void Deserialize(FAssetArchive Ar, long validPos)
    {
        base.Deserialize(Ar, validPos);

        EmoteSound1P = GetOrDefault<USoundCue>(nameof(EmoteSound1P));
        EmoteSound3P = GetOrDefault<USoundCue>(nameof(EmoteSound3P));
    }
}