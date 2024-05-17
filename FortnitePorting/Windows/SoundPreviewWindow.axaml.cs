using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CUE4Parse.UE4.Assets.Exports.Sound;
using FortnitePorting.Services;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Windows;

public partial class SoundPreviewWindow : WindowBase<SoundPreviewViewModel>
{
    public static SoundPreviewWindow? Instance;
    
    public SoundPreviewWindow(USoundWave soundWave)
    {
        InitializeComponent();
        DataContext = ViewModel;
        Owner = ApplicationService.Application.MainWindow;

        ViewModel.SoundName = soundWave.Name;
        ViewModel.SoundWave = soundWave;
        TaskService.Run(ViewModel.Play);
    }

    public static void Preview(USoundWave soundWave)
    {
        if (Instance is not null)
        {
            Instance.ViewModel.SoundName = soundWave.Name;
            Instance.ViewModel.SoundWave = soundWave;
            TaskService.Run(Instance.ViewModel.Play);
            Instance.BringToTop();
            return;
        }

        TaskService.RunDispatcher(() =>
        {
            Instance = new SoundPreviewWindow(soundWave);
            Instance.Show();
            Instance.BringToTop();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance?.ViewModel.OutputDevice.Dispose();
        Instance = null;
    }

    private void OnSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider) return;
        ViewModel.Scrub(TimeSpan.FromSeconds(slider.Value));
    }
}