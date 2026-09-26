using Avalonia;
using Avalonia.Controls;

namespace FortnitePorting.Controls.Leaderboard;

public partial class LeaderboardRankBadge : UserControl
{
    public static readonly StyledProperty<int> RankingProperty =
        AvaloniaProperty.Register<LeaderboardRankBadge, int>(nameof(Ranking));

    public int Ranking
    {
        get => GetValue(RankingProperty);
        set => SetValue(RankingProperty, value);
    }

    public LeaderboardRankBadge()
    {
        InitializeComponent();
    }
}
