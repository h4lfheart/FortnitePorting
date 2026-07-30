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
        TaskService.RunDispatcher(() =>
        {
            if (WindowManager.FindOpen<SoundPreviewWindow>() is { } existing)
            {
                existing.WindowModel.SoundName = soundWave.Name;
                existing.WindowModel.SoundWave = soundWave;
                TaskService.Run(existing.WindowModel.Play);
                existing.BringToTop();
                return;
            }

            WindowManager.GetOrShowPreview(() => new SoundPreviewWindow(soundWave));
        });
    }

    private void OnSliderValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider) return;
        WindowModel.Scrub(TimeSpan.FromSeconds(slider.Value));
    }
}
