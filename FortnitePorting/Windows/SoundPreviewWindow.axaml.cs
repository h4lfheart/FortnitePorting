using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using CUE4Parse.UE4.Assets.Exports.Sound;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class SoundPreviewWindow : PreviewWindowBase<SoundPreviewWindow, SoundPreviewWindowModel>
{
    public SoundPreviewWindow(USoundWave soundWave)
    {
        InitializeComponent();

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
        WindowModel.OutputDevice.Dispose();
        base.OnClosed(e);
    }

    private void OnSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider) return;
        WindowModel.Scrub(TimeSpan.FromSeconds(slider.Value));
    }
}
