using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports.Sound;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Radio;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Windows;
using Material.Icons;
using NAudio.Wave;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class MusicPlayerWindowModel(
    SettingsService settings,
    MusicViewModel music) : WindowModelBase
{
    public SettingsService Settings { get; } = settings;

    public MusicViewModel Music { get; } = music;

    [ObservableProperty] private MusicPackItem? _activeItem;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(PlayIconKind))]
    private bool _isPlaying;

    public MaterialIconKind PlayIconKind => IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(VolumeIconKind))]
    private float _volume = 1.0f;

    public MaterialIconKind VolumeIconKind => Volume switch
    {
        0.0f => MaterialIconKind.VolumeMute,
        < 0.3f => MaterialIconKind.VolumeLow,
        < 0.66f => MaterialIconKind.VolumeMedium,
        <= 1.0f => MaterialIconKind.VolumeHigh
    };

    [ObservableProperty] private ESoundFormat _soundFormat;
    [ObservableProperty] private TimeSpan _currentTime;
    [ObservableProperty] private TimeSpan _totalTime;
    [ObservableProperty] private bool _isLooping;
    [ObservableProperty] private bool _isShuffling;

    public WaveFileReader? AudioReader;
    public WaveOutEvent OutputDevice = new() { DeviceNumber = AppSettings.Application.AudioDeviceIndex };

    private CancellationTokenSource _playbackCts = new();

    private readonly DispatcherTimer _updateTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(1)
    };

    public override async Task Initialize()
    {
        Volume = AppSettings.Application.Volume;
        _updateTimer.Tick += OnUpdateTimerTick;
        _updateTimer.Start();
    }

    public override void OnApplicationExit()
    {
        AppSettings.Application.Volume = Volume;
        MusicPlayerWindow.Instance?.Close();
    }

    private void OnUpdateTimerTick(object? sender, EventArgs e)
    {
        if (AudioReader is null) return;

        TotalTime = AudioReader.TotalTime;
        CurrentTime = AudioReader.CurrentTime;

        if (CurrentTime < TotalTime) return;

        if (IsLooping)
            Restart();
        else
            Next();
    }

    [RelayCommand]
    public void TogglePlayPause()
    {
        if (ActiveItem is null) return;
        if (IsPlaying) Pause();
        else Play();
    }

    [RelayCommand]
    public void Previous()
    {
        if (ActiveItem is null) return;

        var idx = Music.PlaylistMusicPacks.IndexOf(ActiveItem) - 1;
        if (idx < 0) idx = Music.PlaylistMusicPacks.Count - 1;

        if (AudioReader?.CurrentTime.TotalSeconds > 5)
        {
            Restart();
            return;
        }

        CurrentTime = TimeSpan.Zero;
        PlayItem(Music.PlaylistMusicPacks[idx]);
    }

    [RelayCommand]
    public void Next()
    {
        if (ActiveItem is null) return;

        var idx = IsShuffling
            ? Random.Shared.Next(0, Music.PlaylistMusicPacks.Count)
            : Music.PlaylistMusicPacks.IndexOf(ActiveItem) + 1;

        if (idx >= Music.PlaylistMusicPacks.Count) idx = 0;

        CurrentTime = TimeSpan.Zero;
        PlayItem(Music.PlaylistMusicPacks[idx]);
    }

    [RelayCommand]
    public void CloseWindow() => MusicPlayerWindow.Instance?.Close();

    public void PlayItem(MusicPackItem item)
    {
        if (item.IsUnsupported)
        {
            Info.Message("Unsupported Lobby Music Format",
                $"\"{item.TrackName}\" uses a new format for lobby music that is currently unsupported.");
            return;
        }

        if (!SoundExtensions.TrySaveSoundToAssets(
                item.SoundWave.Load<USoundWave>(),
                AppSettings.Application.AssetPath,
                out Stream stream)) return;

        _playbackCts.Cancel();
        _playbackCts = new CancellationTokenSource();
        var cts = _playbackCts;

        Stop(suppressClose: true);

        ActiveItem = item;
        AudioReader = new WaveFileReader(stream);

        Discord.Update($"Listening to \"{ActiveItem.TrackName}\"");

        TaskService.RunDispatcher(MusicPlayerWindow.Open);

        TaskService.Run(() =>
        {
            OutputDevice.Init(AudioReader);
            Play();

            while (OutputDevice.PlaybackState != PlaybackState.Stopped)
            {
                if (cts.IsCancellationRequested) return;
            }

            if (!cts.IsCancellationRequested)
                Stop(suppressClose: false);
        });
    }

    public void Play()
    {
        if (ActiveItem is null) return;
        OutputDevice.Play();
        IsPlaying = true;
        ActiveItem.IsPlaying = true;
    }

    public void Pause()
    {
        if (ActiveItem is null) return;
        OutputDevice.Pause();
        IsPlaying = false;
        ActiveItem.IsPlaying = false;
    }

    public void Stop(bool suppressClose = false)
    {
        if (ActiveItem is null) return;
        OutputDevice.Stop();
        ActiveItem.IsPlaying = false;
        AudioReader?.CurrentTime = TimeSpan.Zero;

        if (!suppressClose && MusicPlayerWindow.Instance is not null)
            TaskService.RunDispatcher(() => MusicPlayerWindow.Instance?.Close());
    }

    public void Restart()
    {
        if (AudioReader is null) return;
        AudioReader.CurrentTime = TimeSpan.Zero;
        OutputDevice.Play();
    }

    public void Scrub(TimeSpan time)
    {
        if (AudioReader is not null)
            AudioReader.CurrentTime = time;
    }

    public void SetVolume(float value) => OutputDevice.Volume = value;

    public void UpdateOutputDevice()
    {
        if (AudioReader is null) return;
        OutputDevice.Stop();
        OutputDevice = new WaveOutEvent { DeviceNumber = AppSettings.Application.AudioDeviceIndex };
        OutputDevice.Init(AudioReader);
        if (IsPlaying)
            OutputDevice.Play();
    }

    partial void OnVolumeChanged(float value) => SetVolume(value);
}