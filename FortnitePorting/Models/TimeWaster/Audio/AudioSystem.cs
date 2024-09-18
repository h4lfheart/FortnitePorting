using System;
using FortnitePorting.ViewModels;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace FortnitePorting.Models.TimeWaster.Audio;

public class AudioSystem : IDisposable
{
    public static readonly AudioSystem Instance = new();

    public int SampleRate;
    public int ChannelCount;
    
    private readonly WaveOutEvent _outputDevice;
    private readonly MixingSampleProvider _mixer;

    public AudioSystem(int sampleRate = 44100, int channelCount = 2)
    {
        SampleRate = sampleRate;
        ChannelCount = channelCount;
        
        _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channelCount))
        {
            ReadFully = true
        };

        _outputDevice = new WaveOutEvent();
        _outputDevice.DesiredLatency = 50;
        _outputDevice.Init(_mixer);
        _outputDevice.Play();
    }
    
    public void PlaySound(ISampleProvider sampleProvider)
    {
        _mixer.AddMixerInput(sampleProvider);
    }
    
    public void PlaySound(IWaveProvider waveProvider)
    {
        _mixer.AddMixerInput(waveProvider);
    }
    
    public void Dispose()
    {
        _outputDevice.Dispose();
    }
}

public static class AudioSystemExtensions
{
    public static void Play(this CachedSound sound)
    {
        AudioSystem.Instance.PlaySound(new WdlResamplingSampleProvider(new CachedSoundSampleProvider(sound), AudioSystem.Instance.SampleRate));
    }
}