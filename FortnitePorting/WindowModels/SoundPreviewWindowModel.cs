using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports.Sound;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.ViewModels;
using Material.Icons;
using NAudio.Wave;

namespace FortnitePorting.WindowModels;

public partial class SoundPreviewWindowModel : WindowModelBase
{
    [ObservableProperty] private string _soundName;
    [ObservableProperty] private USoundWave _soundWave;
    
    [ObservableProperty] private TimeSpan _currentTime;
    [ObservableProperty] private TimeSpan _totalTime;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PauseIcon))] private bool _isPaused;
    public MaterialIconKind PauseIcon => IsPaused ? MaterialIconKind.Play : MaterialIconKind.Pause;

    public WaveFileReader AudioReader;
    public readonly WaveOutEvent OutputDevice = new();
    
    private readonly DispatcherTimer UpdateTimer = new();

    public override async Task Initialize()
    {
        UpdateTimer.Tick += OnUpdateTimerTick;
        UpdateTimer.Interval = TimeSpan.FromMilliseconds(1);
        UpdateTimer.Start();
    }

    private void OnUpdateTimerTick(object? sender, EventArgs e)
    {
        if (AudioReader is null) return;
        
        TotalTime = AudioReader.TotalTime;
        CurrentTime = AudioReader.CurrentTime;
    }

    public async Task Play()
    {
        if (!SoundExtensions.TrySaveSoundToAssets(SoundWave, AppSettings.Current.Application.AssetPath, out Stream stream)) return;

        AudioReader = new WaveFileReader(stream);
        
        OutputDevice.Stop();
        OutputDevice.Init(AudioReader);
        OutputDevice.Play();
        while (OutputDevice.PlaybackState != PlaybackState.Stopped) { }
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        
        if (IsPaused)
        {
            OutputDevice.Pause();
        }
        else
        {
            OutputDevice.Play();
        }
    }

    public void Scrub(TimeSpan time)
    {
        AudioReader.CurrentTime = time;
    }
}