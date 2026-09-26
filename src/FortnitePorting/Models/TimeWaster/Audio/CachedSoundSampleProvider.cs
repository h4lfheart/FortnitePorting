using System;
using NAudio.Wave;

namespace FortnitePorting.Models.TimeWaster.Audio;

public class CachedSoundSampleProvider(CachedSound CachedSound) : ISampleProvider
{
    public WaveFormat WaveFormat => CachedSound.WaveFormat;

    private long _position;

    public int Read(float[] buffer, int offset, int count)
    {
        var availableSamples = CachedSound.AudioData.Length - _position;
        var samplesToCopy = Math.Min(availableSamples, count);
        Array.Copy(CachedSound.AudioData, _position, buffer, offset, samplesToCopy);
        _position += samplesToCopy;
        return (int) samplesToCopy;
    }
}