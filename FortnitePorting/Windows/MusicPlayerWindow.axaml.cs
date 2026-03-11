using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using FortnitePorting.Framework;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class MusicPlayerWindow : WindowBase<MusicPlayerWindowModel>
{
    public static MusicPlayerWindow? Instance;

    public MusicPlayerWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    public static void Open()
    {
        if (Instance is not null)
        {
            Instance.BringToTop();
            return;
        }

        Instance = new MusicPlayerWindow();

        Instance.Show();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        WindowModel.Stop();
        Instance = null;
    }

    private void OnPlaybackSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider slider) return;
        WindowModel.Scrub(TimeSpan.FromSeconds(slider.Value));
    }
}