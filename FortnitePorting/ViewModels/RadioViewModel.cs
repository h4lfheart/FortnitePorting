using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.SoundOut;
using CUE4Parse_Conversion.Sounds;
using FortnitePorting.Controls.Radio;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.Framework.Services;
using Material.Icons;

namespace FortnitePorting.ViewModels;

public partial class RadioViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<RadioSongPicker> loadedSongs = new();
    [ObservableProperty] private RadioSongPicker? songInfo;
    [ObservableProperty] private RuntimeSongInfo? runtimeSongInfo;
    [ObservableProperty] private bool isLooping;
    [ObservableProperty] private bool isShuffling;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(PlayIconKind))]
    private bool isPlaying;

    [ObservableProperty] [NotifyPropertyChangedFor(nameof(VolumeIconKind))]
    private float volume = 1.0f;

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

    private bool HasStarted;
    private const string MusicPackExportType = "AthenaMusicPackItemDefinition";

    public RadioViewModel()
    {
        UpdateTimer.Tick += OnTimerTick;
        UpdateTimer.Interval = TimeSpan.FromSeconds(1);
        UpdateTimer.Start();
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

        var musicPacks = CUE4ParseVM.AssetRegistry.Where(data => data.AssetClass.Text.Equals(MusicPackExportType))
            .ToList();
        foreach (var musicPack in musicPacks)
        {
            if (musicPack.AssetName.Text.Contains("Random", StringComparison.OrdinalIgnoreCase) ||
                musicPack.AssetName.Text.Contains("TBD", StringComparison.OrdinalIgnoreCase)) continue;

            var asset = await CUE4ParseVM.Provider.LoadObjectAsync(musicPack.ObjectPath);
            await TaskService.RunDispatcherAsync(() =>
            {
                var loadedSong = new RadioSongPicker(asset);
                if (LoadedSongs.Any(song => song.Title.Equals(loadedSong.Title))) return;

                LoadedSongs.Add(loadedSong);
            });
        }
    }

    public void Play(RadioSongPicker songPicker)
    {
        var sounds = songPicker.SoundCue.HandleSoundTree();
        var sound = sounds.MaxBy(sound => sound.Time)?.SoundWave;
        if (sound is null) return;

        sound.Decode(true, out var format, out var data);
        if (data is null) sound.Decode(false, out format, out data);
        if (data is null) return;

        switch (format.ToLower())
        {
            case "binka":
                SoundSource = new WaveFileReader(SoundExtensions.ConvertBinkaToWav(data, sound.Name));
                break;
        }

        IsPlaying = true;
        SongInfo = songPicker;
        SoundOut.Stop();
        SoundOut.Initialize(SoundSource);
        SoundOut.Play();
        SoundOut.Volume = Volume;
    }

    public void SetVolume(float value)
    {
        if (!IsValidSong) return;
        SoundOut.Volume = value;
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