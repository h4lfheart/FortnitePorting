using System;
using System.Collections.Generic;
using FortnitePorting.Application;
using FortnitePorting.ViewModels;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace FortnitePorting.Models.TimeWaster.Audio;

public class AudioSystem : IDisposable
{
    public static readonly AudioSystem Instance = new();

    public int SampleRate;
    public int ChannelCount;
    
    private WaveOutEvent _outputDevice;
    private readonly MixingSampleProvider _mixer;

    private Dictionary<string, ISampleProvider> _sampleCache = [];
    
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
    
    public void ReloadOutputDevice()
    {
        _outputDevice?.Stop();
        
        _outputDevice = new WaveOutEvent { DeviceNumber = AppSettings.Current.Application.AudioDeviceIndex };
        _outputDevice.DesiredLatency = 50;
        _outputDevice.Init(_mixer);
        _outputDevice.Play();
    }
    
    public bool Contains(string key)
    {
        return _sampleCache.ContainsKey(key);
    }
    
    public void Cache(string key, ISampleProvider sampleProvider)
    {
        _sampleCache[key] = sampleProvider;
    }
    
    public void PlaySound(string key)
    {
        PlaySound(_sampleCache[key]);
    }
    
    public void StopSound(string key)
    {
        if (!_sampleCache.ContainsKey(key)) return;
        
        StopSound(_sampleCache[key]);
    }
    
    public void PlaySound(ISampleProvider sampleProvider)
    {
        _mixer.AddMixerInput(sampleProvider);
    }
    
    public void StopSound(ISampleProvider sampleProvider)
    {
        _mixer.RemoveMixerInput(sampleProvider);
    }

    public void Stop()
    {
        _mixer.RemoveAllMixerInputs();
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
        AudioSystem.Instance.PlaySound(sound.ToSampleProvider());
    }

    public static WdlResamplingSampleProvider ToSampleProvider(this CachedSound sound)
    {
        ISampleProvider soundProvider = new CachedSoundSampleProvider(sound);
        if (sound.WaveFormat.Channels == 1)
        {
            soundProvider = new MonoToStereoSampleProvider(soundProvider);
        }
        
        return new WdlResamplingSampleProvider(soundProvider, AudioSystem.Instance.SampleRate);
    }
}