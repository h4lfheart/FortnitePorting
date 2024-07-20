using System;
using Avalonia.Media.Imaging;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Leaderboard;

public class LeaderboardUser
{
    public int Ranking { get; set; }
    public Guid Identifier { get; set; }
    public string GlobalName { get; set; }
    public string Username { get; set; }
    public string ProfilePicture { get; set; }
    public int ExportCount { get; set; }

    public Bitmap? MedalBitmap => Ranking <= 3 ? LeaderboardVM.GetMedalBitmap(Ranking) : null;
}