using System;
using Avalonia.Media.Imaging;

namespace FortnitePorting.Models.Leaderboard;

public class LeaderboardStreaksUser
{
    public int Ranking { get; set; }
    public Guid Identifier { get; set; }
    public string GlobalName { get; set; }
    public string Username { get; set; }
    public string ProfilePicture => Globals.GetSeededOGProfileURL(GlobalName);
    public int StreakCount { get; set; }

    public Bitmap? MedalBitmap => Ranking <= 3 ? LeaderboardVM.GetMedalBitmap(Ranking) : null;
}