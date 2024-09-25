using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Controls;

using FortnitePorting.Models.Assets;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using ScottPlot;
using Serilog;

namespace FortnitePorting.Views;

public partial class LeaderboardView : ViewBase<LeaderboardViewModel>
{
    public LeaderboardView()
    {
        InitializeComponent();
    }

    private void OnGraphPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not ContentControl control) return;
        if (control.DataContext is not StatisticsModel statisticsModel) return;

        var pointerPosition = e.GetPosition(control);
        var coordinates = statisticsModel.Graph.Plot.GetCoordinates((float) pointerPosition.X, (float) pointerPosition.Y);
        var nearest = statisticsModel.Scatter.Data.GetNearest(coordinates, statisticsModel.Graph.Plot.LastRender);
        if (nearest.IsReal)
        {
            ValuePopup.IsOpen = true;
            ValuePopup.HorizontalOffset = pointerPosition.X - control.Bounds.Width / 2;
            ValuePopup.VerticalOffset = pointerPosition.Y - control.Bounds.Height / 2;
            ViewModel.PopupValue = (int) nearest.Y;
            statisticsModel.Crosshair.IsVisible = true;
            statisticsModel.Crosshair.Position = new Coordinates(nearest.X, nearest.Y);
            statisticsModel.Graph.Refresh();
        }
        else
        {
            ValuePopup.IsOpen = false;
            statisticsModel.Crosshair.IsVisible = false;
            statisticsModel.Graph.Refresh();
        }
    }
    
    private void OnGraphPointerExited(object? sender, PointerEventArgs e)
    {
        ValuePopup.IsOpen = false;
    }
}