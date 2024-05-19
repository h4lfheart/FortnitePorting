using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class MapView : ViewBase<MapViewModel>
{
    public MapView()
    {
        InitializeComponent();
    }

    private void OnCellPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border) return;

        FlyoutBase.ShowAttachedFlyout(border);
    }
}