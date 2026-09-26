using System;
using System.Linq;
using NAudio.Wave;

namespace FortnitePorting.Services;

public class AudioPlaybackService(SettingsService settings) : IService
{
    public int DeviceIndex => settings.Application.AudioDeviceIndex;

    public float Volume => settings.Application.Volume;

    public DirectSoundDeviceInfo[] Devices => DirectSoundOut.Devices.ToArray()[1..];

    public event Action? OutputDeviceChanged;
    public event Action? VolumeChanged;

    public void NotifyOutputDeviceChanged() => OutputDeviceChanged?.Invoke();

    public void NotifyVolumeChanged() => VolumeChanged?.Invoke();

    public AudioPlaybackSession CreateSession() => new(this);

    public WaveOutEvent CreateOutputDevice(int? desiredLatency = null)
    {
        var output = new WaveOutEvent
        {
            DeviceNumber = DeviceIndex,
            Volume = Volume
        };

        if (desiredLatency is { } latency)
            output.DesiredLatency = latency;

        return output;
    }
}
