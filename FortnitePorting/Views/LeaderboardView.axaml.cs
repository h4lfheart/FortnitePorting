using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class LeaderboardView : ViewBase<LeaderboardViewModel>
{
    public LeaderboardView()
    {
        InitializeComponent();
        
        Navigation.Leaderboard.Initialize(Sidebar, ContentFrame);
    }
    
    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        Navigation.Leaderboard.Open(e.Tag);
    }
}