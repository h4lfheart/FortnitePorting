using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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