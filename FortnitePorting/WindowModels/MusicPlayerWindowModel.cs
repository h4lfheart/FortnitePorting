using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports.Sound;
using FortnitePorting.Application;
using FortnitePorting.Exporting.Extensions;
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
    MusicViewModel music,
    AudioPlaybackService audio) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;

    private readonly MusicViewModel _music = music;

    public AudioPlaybackSession Session { get; } = audio.CreateSession();

    [ObservableProperty] private MusicPackItem? _activeItem;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(PlayIconKind))]
    private bool _isPlaying;

    public MaterialIconKind PlayIconKind => IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play;

    [ObservableProperty] private ESoundFormat _soundFormat;
    [ObservableProperty] private TimeSpan _currentTime;
    [ObservableProperty] private TimeSpan _totalTime;
    [ObservableProperty] private bool _isLooping;
    [ObservableProperty] private bool _isShuffling;

    private CancellationTokenSource _playbackCts = new();

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
        await _playbackCts.CancelAsync();
        _playbackCts.Dispose();
        Session.Dispose();
    }

    public override void OnApplicationExit()
    {
        WindowManager.FindOpen<MusicPlayerWindow>()?.Close();
    }

    private void OnUpdateTimerTick(object? sender, EventArgs e)
    {
        if (Session.Reader is null) return;

        TotalTime = Session.TotalTime;
        CurrentTime = Session.CurrentTime;

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

        var idx = _music.PlaylistMusicPacks.IndexOf(ActiveItem) - 1;
        if (idx < 0) idx = _music.PlaylistMusicPacks.Count - 1;

        if (Session.CurrentTime.TotalSeconds > 5)
        {
            Restart();
            return;
        }

        CurrentTime = TimeSpan.Zero;
        PlayItem(_music.PlaylistMusicPacks[idx]);
    }

    [RelayCommand]
    public void Next()
    {
        if (ActiveItem is null) return;

        var idx = IsShuffling
            ? Random.Shared.Next(0, _music.PlaylistMusicPacks.Count)
            : _music.PlaylistMusicPacks.IndexOf(ActiveItem) + 1;

        if (idx >= _music.PlaylistMusicPacks.Count) idx = 0;

        CurrentTime = TimeSpan.Zero;
        PlayItem(_music.PlaylistMusicPacks[idx]);
    }

    [RelayCommand]
    public void CloseWindow() => Window?.Close();

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
                out Stream stream,
                Dependencies.BinkaDecoderFile,
                Dependencies.RadaDecoderFile,
                Dependencies.VgmStreamFile)) return;

        _playbackCts.Cancel();
        _playbackCts.Dispose();
        _playbackCts = new CancellationTokenSource();
        var cts = _playbackCts;

        Stop(suppressClose: true);

        ActiveItem = item;
        Session.Load(stream);

        Discord.Update($"Listening to \"{ActiveItem.TrackName}\"");

        TaskService.RunDispatcher(() => MusicPlayerWindow.Open());

        TaskService.Run(() =>
        {
            Play();

            while (Session.PlaybackState != PlaybackState.Stopped)
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
        Session.Play();
        IsPlaying = true;
        ActiveItem.IsPlaying = true;
    }

    public void Pause()
    {
        if (ActiveItem is null) return;
        Session.Pause();
        IsPlaying = false;
        ActiveItem.IsPlaying = false;
    }

    public void Stop(bool suppressClose = false)
    {
        if (ActiveItem is null) return;
        Session.Stop();
        ActiveItem.IsPlaying = false;
        IsPlaying = false;
        Session.CurrentTime = TimeSpan.Zero;

        if (!suppressClose && WindowManager.FindOpen<MusicPlayerWindow>() is not null)
            TaskService.RunDispatcher(() => WindowManager.FindOpen<MusicPlayerWindow>()?.Close());
    }

    public void Restart()
    {
        if (Session.Reader is null) return;
        Session.CurrentTime = TimeSpan.Zero;
        Session.Play();
        IsPlaying = true;
        if (ActiveItem is not null)
            ActiveItem.IsPlaying = true;
    }

    public void Scrub(TimeSpan time) => Session.Scrub(time);
}
