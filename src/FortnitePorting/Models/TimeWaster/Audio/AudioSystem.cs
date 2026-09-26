using System;
using FortnitePorting.Application;
using FortnitePorting.Services;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace FortnitePorting.Models.TimeWaster.Audio;

public class AudioSystem : IDisposable
{
    private static readonly Lazy<AudioSystem> LazyInstance = new(() => new AudioSystem());
    public static AudioSystem Instance => LazyInstance.Value;

    public int SampleRate;
    public int ChannelCount;
    
    private WaveOutEvent _outputDevice;
    private readonly MixingSampleProvider _mixer;
    private readonly AudioPlaybackService _audio;

    public AudioSystem(int sampleRate = 44100, int channelCount = 2)
    {
        SampleRate = sampleRate;
        ChannelCount = channelCount;
        _audio = AppServices.Audio;
        
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
        {
            ReadFully = true
        };

        _outputDevice = _audio.CreateOutputDevice(desiredLatency: 50);
        _outputDevice.Init(_mixer);
        _outputDevice.Play();

        _audio.OutputDeviceChanged += OnOutputDeviceChanged;
        _audio.VolumeChanged += OnVolumeChanged;
    }
    
    public void PlaySound(ISampleProvider sampleProvider)
    {
        _mixer.AddMixerInput(sampleProvider);
    }
    
    public void PlaySound(IWaveProvider waveProvider)
    {
        _mixer.AddMixerInput(waveProvider);
    }

    public void Stop()
    {
        _mixer.RemoveAllMixerInputs();
    }
    
    public void Dispose()
    {
        _audio.OutputDeviceChanged -= OnOutputDeviceChanged;
        _audio.VolumeChanged -= OnVolumeChanged;
        _outputDevice.Dispose();
    }

    private void OnVolumeChanged() => _outputDevice.Volume = _audio.Volume;

    private void OnOutputDeviceChanged()
    {
        var wasPlaying = _outputDevice.PlaybackState == PlaybackState.Playing;

        _outputDevice.Stop();
        _outputDevice.Dispose();
        _outputDevice = _audio.CreateOutputDevice(desiredLatency: 50);
        _outputDevice.Init(_mixer);

        if (wasPlaying)
            _outputDevice.Play();
    }
}

public static class AudioSystemExtensions
{
    extension(CachedSound sound)
    {
        public void Play()
        {
            AudioSystem.Instance.PlaySound(new WdlResamplingSampleProvider(new CachedSoundSampleProvider(sound), AudioSystem.Instance.SampleRate));
        }
    }
}
