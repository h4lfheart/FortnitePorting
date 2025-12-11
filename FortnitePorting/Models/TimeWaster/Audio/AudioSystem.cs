using System;
using System.Collections.Generic;
using FluentAvalonia.Core;
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
    
    private WaveOutEvent? _outputDevice;
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

        ReloadOutputDevice();
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
        AudioSystem.Instance.PlaySound(new WdlResamplingSampleProvider(new CachedSoundSampleProvider(sound), AudioSystem.Instance.SampleRate));
    }

    public static WdlResamplingSampleProvider ToSampleProvider(this CachedSound sound)
    {
        return new WdlResamplingSampleProvider(new CachedSoundSampleProvider(sound), AudioSystem.Instance.SampleRate);
    }
}