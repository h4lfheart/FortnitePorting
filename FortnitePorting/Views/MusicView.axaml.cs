using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;
using FortnitePorting.Models.Radio;
using FortnitePorting.ViewModels;

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

        if (ViewModel.ActiveItem == musicPackItem)
        {
            ViewModel.TogglePlayPause();
        }
        else
        {
            ViewModel.Play(musicPackItem);
        }
    }

    private void OnContextMenuPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        
        FlyoutBase.ShowAttachedFlyout(control);
    }
    
    private void OnPlaybackSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider) return;
        ViewModel.Scrub(TimeSpan.FromSeconds(slider.Value));
    }
    
    private void OnVolumeSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider) return;
        ViewModel.SetVolume((float) slider.Value);
    }
    
}