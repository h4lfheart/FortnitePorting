using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Models.SoundCue;

namespace FortnitePorting.Models.SoundCue;

public partial class SoundCueNodeConnection(SoundCueNodeSocket from, SoundCueNodeSocket to) : ObservableObject
{
    [ObservableProperty] private SoundCueNodeSocket _from = from;
    [ObservableProperty] private SoundCueNodeSocket _to = to;

    public override string ToString()
    {
        return $"{From.Name} ({From.Parent.ExpressionName}) -> {To.Name} ({to.Parent.ExpressionName})";
    }
}