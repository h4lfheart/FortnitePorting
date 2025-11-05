using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Controls.Navigation;
using FortnitePorting.Framework;
using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.ViewModels;
using FortnitePorting.ViewModels.Leaderboard;
using FortnitePorting.Views.Leaderboard;
using ScottPlot;
using Serilog;

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