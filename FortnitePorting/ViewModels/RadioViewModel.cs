using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using FortnitePorting.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Radio;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using Material.Icons;
using NAudio.Wave;
using ReactiveUI;
using Serilog;
using FeaturedControl = FortnitePorting.Controls.Home.FeaturedControl;
using NewsControl = FortnitePorting.Controls.Home.NewsControl;

namespace FortnitePorting.ViewModels;

public partial class RadioViewModel : ViewModelBase
{
    [ObservableProperty] private ReadOnlyObservableCollection<MusicPackItem> _activeCollection;
    [ObservableProperty] private string _searchFilter = string.Empty;
    
    [ObservableProperty] private MusicPackItem? _activeItem;
    
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
    
    [ObservableProperty] private TimeSpan _currentTime;
    [ObservableProperty] private TimeSpan _totalTime;
    
    [ObservableProperty] private bool _isLooping;
    [ObservableProperty] private bool _isShuffling;
    
    public WaveFileReader? AudioReader;
    public readonly WaveOutEvent OutputDevice = new();
    
    public readonly ReadOnlyObservableCollection<MusicPackItem> Filtered;
    public readonly ReadOnlyObservableCollection<MusicPackItem> MusicPacks;
    public SourceList<MusicPackItem> Source = new();
    
    private readonly IObservable<Func<MusicPackItem, bool>> RadioItemFilter;

    private readonly string[] IgnoreFilters = ["Random", "TBD", "MusicPack_000_Default"];
    private const string CLASS_NAME = "AthenaMusicPackItemDefinition";
    
    private readonly DispatcherTimer UpdateTimer = new();

    public RadioViewModel()
    {
        UpdateTimer.Tick += OnUpdateTimerTick;
        UpdateTimer.Interval = TimeSpan.FromSeconds(0.1f);
        UpdateTimer.Start();
        
        RadioItemFilter = this.WhenAnyValue(radio => radio.SearchFilter).Select(CreateItemFilter);
        
        Source.Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Sort(SortExpressionComparer<MusicPackItem>.Ascending(item => item.Id))
            .Bind(out MusicPacks)
            .Filter(RadioItemFilter)
            .Bind(out Filtered)
            .Subscribe();

        ActiveCollection = Filtered;
    }

    public override async Task Initialize()
    {
        var assets = CUE4ParseVM.AssetRegistry
            .Where(data => data.AssetClass.Text.Equals(CLASS_NAME))
            .Where(data => !IgnoreFilters.Any(filter => data.AssetName.Text.Contains(filter, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        
        foreach (var asset in assets)
        {
            try
            {
                var musicPack = await CUE4ParseVM.Provider.LoadObjectAsync(asset.ObjectPath);
                Source.Add(new MusicPackItem(musicPack));
            }
            catch (Exception e)
            {
                Log.Error("{0}", e);
            }
        }
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
        if (!SoundExtensions.TrySaveSoundStream(musicPackItem.GetSound(), out var stream)) return;
        
        Stop();
        
        ActiveItem = musicPackItem;
        AudioReader = new WaveFileReader(stream);
        
        TaskService.Run(() =>
        {
            OutputDevice.Init(AudioReader);
            Play();
            
            while (OutputDevice.PlaybackState != PlaybackState.Stopped) { }

            Stop();
        });
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

        var previousSongIndex = MusicPacks.IndexOf(ActiveItem) - 1;
        if (previousSongIndex < 0) previousSongIndex = MusicPacks.Count - 1;
        if (AudioReader?.CurrentTime.TotalSeconds > 5)
        {
            Restart();
            return;
        }
        
        Play(MusicPacks[previousSongIndex]);
    }

    public void Next()
    {
        if (ActiveItem is null) return;
        
        var nextSongIndex = IsShuffling ? Random.Shared.Next(0, MusicPacks.Count) : MusicPacks.IndexOf(ActiveItem) + 1;
        if (nextSongIndex >= MusicPacks.Count)
        {
            nextSongIndex = 0;
        }
        
        Play(MusicPacks[nextSongIndex]);
    }
    
    public void SetVolume(float value)
    {
        OutputDevice.Volume = value;
    }

    [RelayCommand]
    public async Task SaveAll()
    {
        if (await BrowseFolderDialog() is not { } exportPath) return;

        var directory = new DirectoryInfo(exportPath);
        foreach (var item in Source.Items)
        {
            await item.SaveAudio(directory);
        }
    }
    
    private static Func<MusicPackItem, bool> CreateItemFilter(string searchFilter)
    {
        return item => item.Match(searchFilter);
    }
}