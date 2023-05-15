using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class MusicView
{
    private bool IsLooping;
    private bool IsSliderDragging;
    private readonly DispatcherTimer UpdateTimer = new(); 
    public MusicView()
    {
        InitializeComponent();
        AppVM.MusicVM = new MusicViewModel();
        DataContext = AppVM.MusicVM;
        
        UpdateTimer.Tick += OnTick;
        UpdateTimer.Interval = TimeSpan.FromMilliseconds(1);
        UpdateTimer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var info = AppVM.MusicVM.ActiveTrack?.GetInfo();
        if (info is null) return;

        CurrentTime.Text = info.CurrentPosition.ToString(@"mm\:ss");
        TotalTime.Text = info.Length.ToString(@"mm\:ss");
        Slider.Maximum = info.Length.TotalSeconds;
        if (!IsSliderDragging)
            Slider.Value = info.CurrentPosition.TotalSeconds;

        if (info.CurrentPosition >= info.Length)
        {
            if (IsLooping)
            {
                AppVM.MusicVM.ActiveTrack?.Restart();
            }
            else
            {
                AppVM.MusicVM.ContinueQueue();
            }
        }
    }

    private void OnClickPause(object sender, MouseButtonEventArgs e)
    {

        var isPaused = !AppVM.MusicVM.IsPaused;
        AppVM.MusicVM.IsPaused = isPaused;
        
        var text = isPaused ? "resume" : "pause";
        var image = (Image) sender;
        image.Source = new BitmapImage(new Uri($"/FortnitePorting;component/Resources/{text}.png", UriKind.Relative));

        if (isPaused)
        {
            AppVM.MusicVM.Pause();
        }
        else
        {
            AppVM.MusicVM.Resume();
        }
    }

    private void OnClickSkip(object sender, MouseButtonEventArgs e)
    {
        AppVM.MusicVM.ContinueQueue();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        UpdateTimer.Stop();
        AppVM.MusicVM.ActiveTrack?.Dispose();
        DiscordService.ClearMusicState();
    }

    private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        AppVM.MusicVM.ActiveTrack?.Scrub(TimeSpan.FromSeconds(Slider.Value));
    }

    private void OnSliderDragStarted(object sender, DragStartedEventArgs e)
    {
        IsSliderDragging = true;
    }

    private void OnSliderDragCompleted(object sender, DragCompletedEventArgs e)
    {
        IsSliderDragging = false;
    }

    private void OnClickLoop(object sender, MouseButtonEventArgs e)
    {
        var isLooping = !IsLooping;
        IsLooping = isLooping;
        
        var image = (Image) sender;
        image.Opacity = isLooping ? 1.0 : 0.5;
    }
}