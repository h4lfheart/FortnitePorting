using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Extensions;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Supabase.Tables;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Leaderboard;

public partial class LeaderboardStreak : ObservableObject
{
    [ObservableProperty] [JsonProperty("rank")] private int _ranking;
    [ObservableProperty] [JsonProperty("user_id")] private string _userId;

    [ObservableProperty] [JsonProperty("streak")] private int _streak;
    
    [ObservableProperty] private UserInfoResponse? _userInfo;

    public Bitmap? MedalBitmap => Ranking <= 3 ? ImageExtensions.GetMedalBitmap(Ranking) : null;
    
    public SolidColorBrush UserBrush => new(UserInfo?.Role switch
    {
        ESupabaseRole.System => Color.Parse("#B040FF"),
        ESupabaseRole.Owner => Color.Parse("#83c4db"),
        ESupabaseRole.Support => Color.Parse("#635fd4"),
        ESupabaseRole.Staff => Color.Parse("#9856a2"),
        ESupabaseRole.Verified => Color.Parse("#00ff97"),
        ESupabaseRole.User => Colors.White,
        ESupabaseRole.Muted => Color.Parse("#d23940"),
        _ => Colors.White
    });

    public async Task Load()
    {
        UserInfo = await Api.FortnitePorting.UserInfo(UserId);
    }
}