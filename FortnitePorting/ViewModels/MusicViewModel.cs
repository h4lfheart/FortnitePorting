using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.Utils;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Application;
using FortnitePorting.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Radio;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using Material.Icons;
using NAudio.Wave;
using Newtonsoft.Json;
using ReactiveUI;
using Serilog;
using MessageData = FortnitePorting.Models.Information.MessageData;

namespace FortnitePorting.ViewModels;

public partial class MusicViewModel : ViewModelBase
{
    [ObservableProperty] private ReadOnlyObservableCollection<MusicPackItem> _activeCollection;
    [ObservableProperty] private string _searchFilter = string.Empty;
    
    [ObservableProperty] private MusicPackItem? _activeItem;
    [ObservableProperty] private RadioPlaylist _activePlaylist;
    [ObservableProperty] private ObservableCollection<RadioPlaylist> _playlists = [RadioPlaylist.Default];
    public RadioPlaylist[] CustomPlaylists => Playlists.Where(playlist => !playlist.IsDefault).ToArray();
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PlayIconKind))] private bool _isPlaying;
    public MaterialIconKind PlayIconKind => IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play;
    
    [ObservableProperty] [NotifyPropertyChangedFor(nameof(VolumeIconKind))]
    private float volume = 1.0f;
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
    public WaveOutEvent OutputDevice;
    
    public readonly ReadOnlyObservableCollection<MusicPackItem> Filtered;
    public readonly ReadOnlyObservableCollection<MusicPackItem> PlaylistMusicPacks;
    public SourceList<MusicPackItem> Source = new();
    
    private readonly IObservable<Func<MusicPackItem, bool>> RadioSearchFilter;
    private readonly IObservable<Func<MusicPackItem, bool>> RadioPlaylistFilter;

    private readonly string[] IgnoreFilters = ["Random", "TBD", "MusicPack_000_Default"];
    private const string CLASS_NAME = "AthenaMusicPackItemDefinition";
    
    private readonly DispatcherTimer UpdateTimer = new();

    public MusicViewModel()
    {
        UpdateTimer.Tick += OnUpdateTimerTick;
        UpdateTimer.Interval = TimeSpan.FromMilliseconds(1);
        UpdateTimer.Start();
        
        RadioSearchFilter = this.WhenAnyValue(radio => radio.SearchFilter).Select(CreateSearchFilter);
        RadioPlaylistFilter = this.WhenAnyValue(radio => radio.ActivePlaylist).Select(CreatePlaylistFilter);
        
        Source.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            
            .Filter(RadioPlaylistFilter)
            .Sort(SortExpressionComparer<MusicPackItem>.Ascending(item => item.Id))
            .Bind(out PlaylistMusicPacks)
            
            .Filter(RadioSearchFilter)
            .Sort(SortExpressionComparer<MusicPackItem>.Ascending(item => item.Id))
            .Bind(out Filtered)
            .Subscribe();

        ActiveCollection = Filtered;
        OutputDevice = new WaveOutEvent { DeviceNumber = AppSettings.Application.AudioDeviceIndex };
    }

    public override async Task Initialize()
    {
        Volume = AppSettings.Application.Volume;
        foreach (var serializeData in AppSettings.Application.Playlists)
        {
            Playlists.Add(await RadioPlaylist.FromSerializeData(serializeData));
        }
        
        var assets = UEParse.AssetRegistry
            .Where(data => data.AssetClass.Text.Equals(CLASS_NAME))
            .Where(data => !IgnoreFilters.Any(filter => data.AssetName.Text.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        
        foreach (var asset in assets)
        {
            try
            {
                var musicPack = await UEParse.Provider.SafeLoadPackageObjectAsync(asset.ObjectPath);
                Source.Add(new MusicPackItem(musicPack));
            }
            catch (Exception e)
            {
                Log.Error("{0}", e);
            }
        }
    }

    public override void OnApplicationExit()
    {
        base.OnApplicationExit();
        
        AppSettings.Application.Playlists = CustomPlaylists.Select(RadioPlaylistSerializeData.FromPlaylist).ToArray();
        AppSettings.Application.Volume = Volume;
    }

    public override async Task OnViewOpened()
    {
        Discord.Update("Browsing Music");
    }

    private void OnUpdateTimerTick(object? sender, EventArgs e)
    {
        if (AudioReader is null) return;
        
        TotalTime = AudioReader.TotalTime;
        CurrentTime = AudioReader.CurrentTime;
        
        if (CurrentTime >= TotalTime)
        {
            if (IsLooping)
                Restart();
            else
                Next();
        }
    }
    
    public void Play(MusicPackItem musicPackItem)
    {
        if (musicPackItem.IsUnsupported)
        {
            Info.Message("Unsupported Lobby Music Format", $"\"{musicPackItem.TrackName}\" uses a new format for lobby music that is currently unsupported.");
            return;
        }
        
        if (!SoundExtensions.TrySaveSoundToAssets(musicPackItem.SoundWave.Load<USoundWave>(), AppSettings.Application.AssetPath, out Stream stream)) return;
        
        Stop();

        ActiveItem = musicPackItem;
        AudioReader = new WaveFileReader(stream);
        
        Discord.Update($"Listening to \"{ActiveItem.TrackName}\"");
        
        TaskService.Run(() =>
        {
            OutputDevice.Init(AudioReader);
            Play();
            
            while (OutputDevice.PlaybackState != PlaybackState.Stopped) { }

            Stop();
        });
    }

    public void UpdateOutputDevice()
    {
        OutputDevice.Stop();
        OutputDevice = new WaveOutEvent { DeviceNumber = AppSettings.Application.AudioDeviceIndex };
        OutputDevice.Init(AudioReader);
        
        if (IsPlaying && AudioReader is not null)
        {
            OutputDevice.Play();
        }
    }
    

    public void Scrub(TimeSpan time)
    {
        if (AudioReader is null) return;
        AudioReader.CurrentTime = time;
    }
    
    public void Restart()
    {
        if (AudioReader is null) return;
        AudioReader.CurrentTime = TimeSpan.Zero;
        OutputDevice.Play();
    }
    
    public void TogglePlayPause()
    {
        if (ActiveItem is null) return;
        
        if (IsPlaying)
            Pause();
        else
            Play();
    }
    
    public void Stop()
    {
        if (ActiveItem is null) return;
        
        OutputDevice.Stop();
        ActiveItem.IsPlaying = false;
    }

    public void Pause()
    {
        if (ActiveItem is null) return;
        
        OutputDevice.Pause();
        IsPlaying = false;
        ActiveItem.IsPlaying = false;
    }

    public void Play()
    {
        if (ActiveItem is null) return;
        
        OutputDevice.Play();
        IsPlaying = true;
        ActiveItem.IsPlaying = true;
    }
    
    public void Previous()
    {
        if (ActiveItem is null) return;

        var previousSongIndex = PlaylistMusicPacks.IndexOf(ActiveItem) - 1;
        if (previousSongIndex < 0) previousSongIndex = PlaylistMusicPacks.Count - 1;
        if (AudioReader?.CurrentTime.TotalSeconds > 5)
        {
            Restart();
            return;
        }
        
        CurrentTime = TimeSpan.Zero;
        Play(PlaylistMusicPacks[previousSongIndex]);
    }

    public void Next()
    {
        if (ActiveItem is null) return;
        
        var nextSongIndex = IsShuffling ? Random.Shared.Next(0, PlaylistMusicPacks.Count) : PlaylistMusicPacks.IndexOf(ActiveItem) + 1;
        if (nextSongIndex >= PlaylistMusicPacks.Count)
        {
            nextSongIndex = 0;
        }
        
        CurrentTime = TimeSpan.Zero;
        Play(PlaylistMusicPacks[nextSongIndex]);
    }
    
    public void SetVolume(float value)
    {
        OutputDevice.Volume = value;
    }

    [RelayCommand]
    public async Task SaveAll()
    {
        if (await App.BrowseFolderDialog() is not { } exportPath) return;

        var directory = new DirectoryInfo(exportPath);

        var infoBar = new MessageData("Music Packs", "Exporting...", autoClose: false, id: "RadioExportAll");
        Info.Message(infoBar);

        var exportItems = Source.Items.ToArray();
        var currentItemIndex = -1;
        foreach (var item in exportItems)
        {
            currentItemIndex++;
            if (item.IsUnsupported) continue;
            
            Info.UpdateMessage("RadioExportAll", $"Exporting {item.TrackName}: {currentItemIndex} / {exportItems.Length}");
            await item.SaveAudio(directory, SoundFormat);
        }
        
        Info.CloseMessage("RadioExportAll");
    }
    
    [RelayCommand]
    public async Task AddPlaylist()
    {
        Playlists.Add(new RadioPlaylist(isDefault: false));
    }

    [RelayCommand]
    public async Task RemovePlaylist()
    {
        if (ActivePlaylist.IsDefault) return;
        
        Playlists.Remove(ActivePlaylist);
        ActivePlaylist = Playlists.Last();
    }
    
    [RelayCommand]
    public async Task ExportPlaylist()
    {
        if (ActivePlaylist.IsDefault) return;
        if (await App.SaveFileDialog(suggestedFileName: ActivePlaylist.PlaylistName, fileTypes: Globals.PlaylistFileType) is not { } path) return;

        path = path.SubstringBeforeLast(".").SubstringBeforeLast("."); // scuffed fix for avalonia bug
        var serializeData = RadioPlaylistSerializeData.FromPlaylist(ActivePlaylist);
        await File.WriteAllTextAsync(path, JsonConvert.SerializeObject(serializeData));
    }
    
    [RelayCommand]
    public async Task ImportPlaylist()
    {
        if (await App.BrowseFileDialog(fileTypes: Globals.PlaylistFileType) is not { } path) return;

        var serializeData = JsonConvert.DeserializeObject<RadioPlaylistSerializeData>(await File.ReadAllTextAsync(path));
        if (serializeData is null) return;

        var playlist = await RadioPlaylist.FromSerializeData(serializeData);
        Playlists.Add(playlist);
    }
    
    private static Func<MusicPackItem, bool> CreateSearchFilter(string searchFilter)
    {
        return item => item.Match(searchFilter);
    }
    
    private static Func<MusicPackItem, bool> CreatePlaylistFilter(RadioPlaylist playlist)
    {
        if (playlist is null) return _ => true;
        
        return item => playlist.ContainsID(item.Id);
    }
}