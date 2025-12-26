using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Levels;

public partial class LevelStats : ObservableObject
{
    [ObservableProperty] [JsonProperty("current_level")] private int _level;
    [ObservableProperty] [JsonProperty("total_xp")] private long _totalXP;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(XPFractionText))] [JsonProperty("xp_for_next_level")] private string _xPForNextLevel;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(XPFractionText))] [JsonProperty("xp_into_level")] private string _xPIntoNextLevel;
    
    public string XPFractionText => $"{XPIntoNextLevel}/{XPForNextLevel} XP";
}