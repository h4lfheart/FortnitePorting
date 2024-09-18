using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;
using NAudio.Vorbis;
using NAudio.Wave;

namespace FortnitePorting.Models.TimeWaster.Audio;

public class CachedSound
{
    public readonly float[] AudioData;
    public readonly WaveFormat WaveFormat;
    
    public CachedSound(string resourcePath)
    {
        using var stream = AssetLoader.Open(new Uri(resourcePath));
        using var audioFileReader = new VorbisWaveReader(stream);
        
        WaveFormat = audioFileReader.WaveFormat;
        
        var audioData = new List<float>((int)(audioFileReader.Length / 4));
        var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
        
        int samplesRead;
        while ((samplesRead = audioFileReader.Read(readBuffer,0,readBuffer.Length)) > 0)
        {
            audioData.AddRange(readBuffer.Take(samplesRead));
        }
        
        AudioData = audioData.ToArray();
    }
}