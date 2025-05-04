using System;
using System.Threading.Tasks;
using Windows.System.UserProfile;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Extensions;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Shared.Extensions;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Leaderboard;

public partial class LeaderboardUser : ObservableObject
{
    [ObservableProperty] [JsonProperty("rank")] private int _ranking;
    [ObservableProperty] [JsonProperty("user_id")] private string _userId;

    [ObservableProperty] [JsonProperty("total")] private int _exportCount;

    [ObservableProperty] private UserInfoResponse? _userInfo;

    public Bitmap? MedalBitmap => Ranking <= 3 ? ImageExtensions.GetMedalBitmap(Ranking) : null;

    public async Task Load()
    {
        UserInfo = await Api.FortnitePortingV2.UserInfo(UserId);
    }
}