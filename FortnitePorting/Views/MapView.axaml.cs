using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class MapView : ViewBase<MapViewModel>
{
    public MapView()
    {
        InitializeComponent();
    }

    private void OnCellHoveredOver(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;
        if (border.DataContext is not WorldPartitionGrid grid) return;
        grid.IsSelected = !grid.IsSelected;

        if (grid.IsSelected)
        {
            ViewModel.SelectedMaps.AddRange(grid.Maps);
        }
        else
        {
            foreach (var map in grid.Maps)
            {
                ViewModel.SelectedMaps.Remove(map);
            }
        }

        //FlyoutBase.ShowAttachedFlyout(border);
    }
}