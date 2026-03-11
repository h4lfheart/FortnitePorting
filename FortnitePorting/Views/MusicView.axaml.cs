using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using FortnitePorting.Models.Radio;
using FortnitePorting.ViewModels;
using FortnitePorting.Windows;

namespace FortnitePorting.Views;

public partial class MusicView : ViewBase<MusicViewModel>
{
    public MusicView()
    {
        InitializeComponent();
    }

    private void OnPlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        if (sender is not Control control) return;
        if (control.DataContext is not MusicPackItem musicPackItem) return;
        
        MusicPlayerWindow.Open();
        if (MusicPlayerWindow.Instance?.WindowModel is not { } player) return;

        if (player.ActiveItem == musicPackItem)
            player.TogglePlayPause();
        else
            player.PlayItem(musicPackItem);
    }

    private void OnContextMenuPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        FlyoutBase.ShowAttachedFlyout(control);
    }
}