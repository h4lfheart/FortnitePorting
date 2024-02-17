using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ATL;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.SoundOut;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Application;
using FortnitePorting.Controls.Radio;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.Framework.Services;
using Material.Icons;
using ReactiveUI;

namespace FortnitePorting.ViewModels;

public partial class RadioViewModel : ViewModelBase
{
    [ObservableProperty] private RadioSongPicker? songInfo;
    [ObservableProperty] private RuntimeSongInfo? runtimeSongInfo;
    [ObservableProperty] private bool isLooping;
    [ObservableProperty] private bool isShuffling;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(PlayIconKind))]
    private bool isPlaying;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(VolumeIconKind))]
    private float volume = 1.0f;
    
    [ObservableProperty] private int loaded;
    [ObservableProperty] private int total;
    
    [ObservableProperty] private string searchFilter = string.Empty;
    [ObservableProperty] private ReadOnlyObservableCollection<RadioSongPicker> loadedSongs;
    [ObservableProperty] private ReadOnlyObservableCollection<RadioSongPicker> radioItemsTarget;
    [ObservableProperty] private SourceList<RadioSongPicker> radioItemsSource = new();
    private readonly IObservable<Func<RadioSongPicker, bool>> RadioItemFilter;
    
    public bool IsValidSong => SoundSource is not null && SongInfo is not null && RuntimeSongInfo is not null;
    public MaterialIconKind PlayIconKind => IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play;

    public MaterialIconKind VolumeIconKind => Volume switch
    {
        0.0f => MaterialIconKind.VolumeMute,
        < 0.3f => MaterialIconKind.VolumeLow,
        < 0.66f => MaterialIconKind.VolumeMedium,
        <= 1.0f => MaterialIconKind.VolumeHigh
    };

    private IWaveSource? SoundSource;
    private readonly ISoundOut SoundOut = SoundExtensions.GetSoundOut();
    private readonly DispatcherTimer UpdateTimer = new();
    private readonly string[] IgnoreFilters = ["Random", "TBD"];

    private bool HasStarted;
    private const string MusicPackExportType = "AthenaMusicPackItemDefinition";

    public RadioViewModel()
    {
        UpdateTimer.Tick += OnTimerTick;
        UpdateTimer.Interval = TimeSpan.FromSeconds(1);
        UpdateTimer.Start();

        RadioItemFilter = this.WhenAnyValue(x => x.SearchFilter).Select(CreateRadioFilter);
        RadioItemsSource.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Sort(SortExpressionComparer<RadioSongPicker>.Ascending(Radio => Radio.ID))
            .Bind(out var sourceTarget)
            .Filter(RadioItemFilter)
            .Bind(out var target)
            .Subscribe();
        LoadedSongs = sourceTarget;
        RadioItemsTarget = target;
        Volume = AppSettings.Current.RadioVolume;
    }
    
    private Func<RadioSongPicker, bool> CreateRadioFilter(string filter)
    {
        return asset => MiscExtensions.Filter(asset.Title, filter);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (SoundSource is null)
        {
            RuntimeSongInfo = null;
            return;
        }

        RuntimeSongInfo = new RuntimeSongInfo
        {
            Position = SoundSource.GetPosition(),
            Length = SoundSource.GetLength()
        };

        if (RuntimeSongInfo.Position >= RuntimeSongInfo.Length)
        {
            if (IsLooping)
                Restart();
            else
                Next();
        }
    }

    public override async Task Initialize()
    {
        if (HasStarted) return;
        HasStarted = true;

        var musicPacks = CUE4ParseVM.AssetRegistry
            .Where(data => data.AssetClass.Text.Equals(MusicPackExportType) && !IgnoreFilters.Any(filter => data.AssetName.Text.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        Total = musicPacks.Count;
        foreach (var musicPack in musicPacks)
        {
            var asset = await CUE4ParseVM.Provider.LoadObjectAsync(musicPack.ObjectPath);
            await TaskService.RunDispatcherAsync(() =>
            {
                var loadedSong = new RadioSongPicker(asset);
                if (RadioItemsSource.Items.Any(song => song.Title.Equals(loadedSong.Title))) return;

                RadioItemsSource.Add(loadedSong);
            });
            Loaded++;
        }
    }

    public void Play(RadioSongPicker songPicker)
    {
        TaskService.Run(() =>
        {
            var sound = songPicker.GetSound();
            if (sound is null) return;

            var wavPath = Path.Combine(AudioCacheFolder.FullName, $"{sound.Name}.wav");
            if (File.Exists(wavPath))
            {
                SoundSource = new WaveFileReader(wavPath);
            }
            else if (SoundExtensions.TrySaveAudioStream(sound, wavPath, out var stream))
            {
                SoundSource = new WaveFileReader(stream);
            }
            else
            {
                return;
            }

            IsPlaying = true;
            SongInfo = songPicker;
            SoundOut.Stop();
            SoundOut.Initialize(SoundSource);
            SoundOut.Play();
            SoundOut.Volume = Volume;
        
            DiscordService.UpdateMusic(songPicker.Title);
        });
    }

    public void SetVolume(float value)
    {
        if (!IsValidSong) return;
        SoundOut.Volume = value;
        AppSettings.Current.RadioVolume = SoundOut.Volume;
    }

    public void TogglePlayPause()
    {
        if (!IsValidSong) return;

        if (IsPlaying)
            Pause();
        else
            Resume();
    }

    public void Stop()
    {
        if (!IsValidSong) return;
        SoundOut.Stop();
    }

    public void Pause()
    {
        if (!IsValidSong) return;
        SoundOut.Pause();
        IsPlaying = false;
    }

    public void Resume()
    {
        if (!IsValidSong) return;
        SoundOut.Resume();
        IsPlaying = true;
    }

    public void Previous()
    {
        if (!IsValidSong) return;

        var previousSongIndex = LoadedSongs.IndexOf(SongInfo) - 1;
        if (RuntimeSongInfo?.Position.TotalSeconds > 5 || previousSongIndex < 0)
        {
            Restart();
            return;
        }
        
        Play(LoadedSongs[previousSongIndex]);
    }

    public void Next()
    {
        if (!IsValidSong) return;
        var nextSongIndex = IsShuffling ? Random.Shared.Next(0, LoadedSongs.Count) : LoadedSongs.IndexOf(SongInfo) + 1;
        if (nextSongIndex >= LoadedSongs.Count)
        {
            nextSongIndex = 0;
        }
        Play(LoadedSongs[nextSongIndex]);
    }

    public void Restart()
    {
        if (!IsValidSong) return;
        SoundSource?.SetPosition(TimeSpan.Zero);
        SoundOut.Play();
    }

    public void Scrub(TimeSpan time)
    {
        if (!IsValidSong) return;
        SoundSource?.SetPosition(time);
    }
}

public class RuntimeSongInfo
{
    public TimeSpan Length { get; set; }
    public TimeSpan Position { get; set; }
}