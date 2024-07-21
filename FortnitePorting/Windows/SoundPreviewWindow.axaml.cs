using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CUE4Parse.UE4.Assets.Exports.Sound;
using FortnitePorting.Services;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class SoundPreviewWindow : WindowBase<SoundPreviewWindowModel>
{
    public static SoundPreviewWindow? Instance;
    
    public SoundPreviewWindow(USoundWave soundWave)
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = ApplicationService.Application.MainWindow;

        WindowModel.SoundName = soundWave.Name;
        WindowModel.SoundWave = soundWave;
        TaskService.Run(WindowModel.Play);
    }

    public static void Preview(USoundWave soundWave)
    {
        if (Instance is not null)
        {
            Instance.WindowModel.SoundName = soundWave.Name;
            Instance.WindowModel.SoundWave = soundWave;
            TaskService.Run(Instance.WindowModel.Play);
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

        Instance?.WindowModel.OutputDevice.Dispose();
        Instance = null;
    }

    private void OnSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider) return;
        WindowModel.Scrub(TimeSpan.FromSeconds(slider.Value));
    }
}