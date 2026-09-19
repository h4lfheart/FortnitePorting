using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace FortnitePorting.Controls.Leaderboard;

public partial class LeaderboardPagination : UserControl
{
    public static readonly StyledProperty<string?> PageInfoProperty =
        AvaloniaProperty.Register<LeaderboardPagination, string?>(nameof(PageInfo));

    public string? PageInfo
    {
        get => GetValue(PageInfoProperty);
        set => SetValue(PageInfoProperty, value);
    }

    public static readonly StyledProperty<ICommand?> PreviousPageCommandProperty =
        AvaloniaProperty.Register<LeaderboardPagination, ICommand?>(nameof(PreviousPageCommand));

    public ICommand? PreviousPageCommand
    {
        get => GetValue(PreviousPageCommandProperty);
        set => SetValue(PreviousPageCommandProperty, value);
    }

    public static readonly StyledProperty<ICommand?> NextPageCommandProperty =
        AvaloniaProperty.Register<LeaderboardPagination, ICommand?>(nameof(NextPageCommand));

    public ICommand? NextPageCommand
    {
        get => GetValue(NextPageCommandProperty);
        set => SetValue(NextPageCommandProperty, value);
    }

    public LeaderboardPagination()
    {
        InitializeComponent();
    }
}
