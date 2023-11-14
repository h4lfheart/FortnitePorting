using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Documents;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CSCore;
using CSCore.Codecs.WAV;
using CSCore.SoundOut;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using FortnitePorting.Controls.Radio;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using Material.Icons;
using Serilog;

namespace FortnitePorting.ViewModels;

public partial class RadioViewModel : ViewModelBase
{
    [ObservableProperty] private ObservableCollection<RadioSongPicker> loadedSongs = new();
    [ObservableProperty] private RadioSongPicker? songInfo;
    [ObservableProperty] private RuntimeSongInfo? runtimeSongInfo;
    private readonly DispatcherTimer UpdateTimer = new();
    
    private IWaveSource? SoundSource;
    private readonly ISoundOut SoundOut = SoundExtensions.GetSoundOut();

    [ObservableProperty, NotifyPropertyChangedFor(nameof(PlayIconKind))] private bool isPaused;
    public MaterialIconKind PlayIconKind => IsPaused ? MaterialIconKind.Play : MaterialIconKind.Pause;

    private bool HasStarted;
    private const string MusicPackExportType = "AthenaMusicPackItemDefinition";

    public RadioViewModel()
    {
        UpdateTimer.Tick += OnTimerTick;
        UpdateTimer.Interval = TimeSpan.FromMilliseconds(1);
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
            // todo looping support
            // todo onto next track in queue
            Restart();
            SoundOut.Play();
        }
    }

    public override async Task Initialize()
    {
        if (HasStarted) return;
        HasStarted = true;
        
        var musicPacks = CUE4ParseVM.AssetRegistry.Where(data => data.AssetClass.Text.Equals(MusicPackExportType)).ToList();
        foreach (var musicPack in musicPacks)
        {
            if (musicPack.AssetName.Text.Contains("Random", StringComparison.OrdinalIgnoreCase) || musicPack.AssetName.Text.Contains("TBD", StringComparison.OrdinalIgnoreCase)) continue;
            
            try
            {
                var asset = await CUE4ParseVM.Provider.LoadObjectAsync(musicPack.ObjectPath);
                await TaskService.RunDispatcherAsync(() => LoadedSongs.Add(new RadioSongPicker(asset)));
            }
            catch (Exception e)
            {
                Log.Error("{0}", e);
            }
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
                SoundSource = new WaveFileReader(SoundExtensions.ConvertBinkaToWav(data));
                break;
        }
        
        SoundOut.Stop();
        SoundOut.Initialize(SoundSource);
        SoundOut.Play();
        SongInfo = songPicker;
    }
    
    public void Pause()
    {
        SoundOut.Pause();
    }

    public void Resume()
    {
        SoundOut.Resume();
    }

    public void Restart()
    {
        SoundSource?.SetPosition(TimeSpan.Zero);
    }

    public void Scrub(TimeSpan time)
    {
        SoundSource?.SetPosition(time);
    }
}

public class RuntimeSongInfo
{
    public TimeSpan Length { get; set; }
    public TimeSpan Position { get; set; }
}