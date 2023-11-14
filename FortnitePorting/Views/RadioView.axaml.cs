using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class RadioView : ViewBase<RadioViewModel>
{
    public RadioView() : base(lateInit: true)
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        await ViewModel.Initialize();
    }

    private void OnPausePlayPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.IsPaused = !ViewModel.IsPaused;
        if (ViewModel.IsPaused)
        {
            ViewModel.Pause();
        }
        else
        {
            ViewModel.Resume();
        }
    }
    
    private void OnRestartPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.Restart();
    }

    private void OnSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider) return;
        ViewModel.Scrub(TimeSpan.FromSeconds(slider.Value));
    }
}