using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.UE4.Assets.Exports.Sound;
using FortnitePorting.Application;
using FortnitePorting.Exporting.Extensions;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using Material.Icons;
using NAudio.Wave;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class SoundPreviewWindowModel(
    SettingsService settings,
    AudioPlaybackService audio) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;
    
    [ObservableProperty] private string _soundName = string.Empty;
    [ObservableProperty] private USoundWave? _soundWave;
    
    [ObservableProperty] private TimeSpan _currentTime;
    [ObservableProperty] private TimeSpan _totalTime;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PauseIcon))] private bool _isPaused;
    public MaterialIconKind PauseIcon => IsPaused ? MaterialIconKind.Play : MaterialIconKind.Pause;

    public AudioPlaybackSession Session { get; } = audio.CreateSession();

    private readonly DispatcherTimer _updateTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(1)
    };

    public override async Task Initialize()
    {
        _updateTimer.Tick += OnUpdateTimerTick;
        _updateTimer.Start();
    }

    public override async Task OnViewExited()
    {
        _updateTimer.Stop();
        _updateTimer.Tick -= OnUpdateTimerTick;
        Session.Dispose();
    }

    private void OnUpdateTimerTick(object? sender, EventArgs e)
    {
        if (Session.Reader is null) return;
        
        TotalTime = Session.TotalTime;
        CurrentTime = Session.CurrentTime;
    }

    public async Task Play()
    {
        if (SoundWave is null) return;
        if (!SoundExtensions.TrySaveSoundToAssets(SoundWave, AppSettings.Application.AssetPath, out Stream stream,
                Dependencies.BinkaDecoderFile, Dependencies.RadaDecoderFile, Dependencies.VgmStreamFile)) return;

        IsPaused = false;
        Session.Load(stream);
        Session.Play();

        while (Session.PlaybackState != PlaybackState.Stopped)
            await Task.Delay(25);
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;
        
        if (IsPaused)
            Session.Pause();
        else
            Session.Play();
    }

    public void Scrub(TimeSpan time) => Session.Scrub(time);
}
