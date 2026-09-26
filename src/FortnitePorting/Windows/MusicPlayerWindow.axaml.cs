using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using FortnitePorting.Framework;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class MusicPlayerWindow : WindowBase<MusicPlayerWindowModel>, IPreviewWindow
{
    public MusicPlayerWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    public static MusicPlayerWindow Open()
    {
        return WindowManager.GetOrShowPreview(() => new MusicPlayerWindow());
    }

    protected override void OnClosed(EventArgs e)
    {
        WindowModel.Stop(suppressClose: true);
        base.OnClosed(e);
    }

    private void OnPlaybackSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider) return;
        WindowModel.Scrub(TimeSpan.FromSeconds(slider.Value));
    }
}
