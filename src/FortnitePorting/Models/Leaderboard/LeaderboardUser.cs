using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Extensions;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Supabase.Tables;
using System.Text.Json.Serialization;

namespace FortnitePorting.Models.Leaderboard;

public partial class LeaderboardUser : ObservableObject
{
    [ObservableProperty] [property: JsonPropertyName("rank")] private int _ranking;
    [ObservableProperty] [property: JsonPropertyName("user_id")] private string _userId;

    [ObservableProperty] [property: JsonPropertyName("total")] private int _exportCount;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(UserBrush))]
    private UserInfoResponse? _userInfo;

    public SolidColorBrush UserBrush => UserInfo?.Role.Brush() ?? ESupabaseRole.User.Brush();

    public async Task Load()
    {
        UserInfo = await SupaBase.GetUserAsync(UserId);
    }
}
