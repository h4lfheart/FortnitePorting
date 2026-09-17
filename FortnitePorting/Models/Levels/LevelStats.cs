using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace FortnitePorting.Models.Levels;

public partial class LevelStats : ObservableObject
{
    [ObservableProperty] [property: JsonPropertyName("current_level")] private int _level;
    [ObservableProperty] [property: JsonPropertyName("total_xp")] private long _totalXP;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(XPFractionText))] [property: JsonPropertyName("xp_for_next_level")] private int _xPForNextLevel;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(XPFractionText))] [property: JsonPropertyName("xp_into_level")] private int _xPIntoNextLevel;
    
    public string XPFractionText => $"{XPIntoNextLevel}/{XPForNextLevel} XP";
}