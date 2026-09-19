using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Extensions;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Supabase.Tables;
using System.Text.Json.Serialization;

namespace FortnitePorting.Models.Leaderboard;

public partial class LeaderboardStats : ObservableObject
{
    [ObservableProperty] [property: JsonPropertyName("current_streak")] private int _currentStreak;
    [ObservableProperty] [property: JsonPropertyName("total_exports")] private int _totalExports;
    [ObservableProperty] [property: JsonPropertyName("total_assets")] private int _totalAssets;
    [ObservableProperty] [property: JsonPropertyName("most_exported_object_path")] private string _mostPopularObjectPath;
    [ObservableProperty] [property: JsonPropertyName("most_exported_object_count")] private int _mostPopularObjectCount;
    [ObservableProperty] [property: JsonPropertyName("total_xp")] private long _totalXP;

}