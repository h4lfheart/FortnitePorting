using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;
using CSCore;
using CSCore.Codecs.OGG;
using CSCore.Codecs.WAV;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Sound.Node;
using FortnitePorting.Exports;
using FortnitePorting.Services;

namespace FortnitePorting.Views.Controls;

public partial class MusicQueueItem : IDisposable
{
    public ImageSource MusicImageSource { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }

    private readonly UObject Asset;
    private ISoundOut? SoundOut;
    private IWaveSource? SoundSource;
    private static readonly MMDeviceEnumerator DeviceEnumerator = new();

    public MusicQueueItem(IExportableAsset musicPackItem)
    {
        InitializeComponent();
        DataContext = this;

        MusicImageSource = musicPackItem.FullSource;
        DisplayName = musicPackItem.DisplayName;
        Description = musicPackItem.Description;
        Asset = musicPackItem.Asset;
    }

    public MusicQueueItem(UObject asset, ImageSource source, string displayName, string description)
    {
        InitializeComponent();
        DataContext = this;

        MusicImageSource = source;
        DisplayName = displayName;
        Description = description;
        Asset = asset;
    }

    public static USoundWave GetProperSoundWave(UObject asset)
    {
        var musicCue = asset.Get<USoundCue>("FrontEndLobbyMusic");
        var sounds = ExportHelpers.HandleAudioTree(musicCue.FirstNode!.Load<USoundNode>()!);
        var properSound = sounds.MaxBy(sound => sound.Time);
        return properSound?.SoundWave;
    }

    public void Initialize()
    {
        var properSoundWave = GetProperSoundWave(Asset);
        properSoundWave.Decode(true, out var format, out var data);
        if (data is null) return;

        switch (format.ToLower())
        {
            case "ogg":
                SoundSource = new OggSource(new MemoryStream(data)).ToWaveSource();
                break;
            case "adpcm":
                SoundSource = new WaveFileReader(ConvertedData(data));
                break;
        }

        SoundOut = GetSoundOut();
        SoundOut.Initialize(SoundSource);
        SoundOut.Play();

        DiscordService.UpdateMusicState(DisplayName);
    }

    private MemoryStream ConvertedData(byte[] data)
    {
        var vgmPath = Path.Combine(App.VGMStreamFolder.FullName, "vgmstream-cli.exe");

        var adpcmPath = Path.Combine(App.VGMStreamFolder.FullName, "temp.adpcm");
        var wavPath = Path.ChangeExtension(adpcmPath, ".wav");

        File.WriteAllBytes(adpcmPath, data);

        var vgmInst = Process.Start(new ProcessStartInfo
        {
            FileName = vgmPath,
            Arguments = $"-o \"{wavPath}\" \"{adpcmPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });
        vgmInst?.WaitForExit();

        var memoryStream = new MemoryStream(File.ReadAllBytes(wavPath));

        File.Delete(adpcmPath);
        File.Delete(wavPath);

        return memoryStream;
    }

    public MusicPackRuntimeInfo GetInfo()
    {
        return new MusicPackRuntimeInfo
        {
            Length = SoundSource.GetLength(),
            CurrentPosition = SoundSource.GetPosition()
        };
    }

    public void Pause()
    {
        SoundOut?.Pause();
    }

    public void Resume()
    {
        SoundOut?.Resume();
    }

    public void Restart()
    {
        SoundSource.SetPosition(TimeSpan.Zero);
    }

    public void Scrub(TimeSpan time)
    {
        SoundSource.SetPosition(time);
    }

    public void Dispose()
    {
        SoundOut?.Stop();
        SoundOut?.Dispose();
        SoundSource?.Dispose();
    }

    private static ISoundOut GetSoundOut()
    {
        if (WasapiOut.IsSupportedOnCurrentPlatform)
        {
            return new WasapiOut
            {
                Device = GetDevice()
            };
        }

        return new DirectSoundOut();
    }

    private static MMDevice GetDevice()
    {
        return DeviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);
    }
}

public class MusicPackRuntimeInfo
{
    public TimeSpan Length;
    public TimeSpan CurrentPosition;
}