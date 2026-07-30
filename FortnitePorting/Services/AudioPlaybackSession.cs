using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Wave;

namespace FortnitePorting.Services;

public sealed partial class AudioPlaybackSession : ObservableObject, IDisposable
{
    private readonly AudioPlaybackService _audio;
    private WaveOutEvent _output;
    private WaveStream? _reader;
    private bool _disposed;

    [ObservableProperty] private float _volume;

    public WaveStream? Reader => _reader;

    public PlaybackState PlaybackState => _output.PlaybackState;

    public TimeSpan CurrentTime
    {
        get => _reader?.CurrentTime ?? TimeSpan.Zero;
        set
        {
            if (_reader is not null)
                _reader.CurrentTime = value;
        }
    }

    public TimeSpan TotalTime => _reader?.TotalTime ?? TimeSpan.Zero;

    public AudioPlaybackSession(AudioPlaybackService audio)
    {
        _audio = audio;
        _output = audio.CreateOutputDevice();
        Volume = audio.Volume;
        _audio.OutputDeviceChanged += OnOutputDeviceChanged;
        _audio.VolumeChanged += OnServiceVolumeChanged;
    }

    public void Load(Stream stream)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _output.Stop();
        _reader?.Dispose();
        _reader = new WaveFileReader(stream);
        _output.Init(_reader);
    }

    public void Play()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _output.Play();
    }

    public void Pause()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _output.Pause();
    }

    public void Stop()
    {
        if (_disposed) return;
        _output.Stop();
    }

    public void Scrub(TimeSpan time) => CurrentTime = time;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _audio.OutputDeviceChanged -= OnOutputDeviceChanged;
        _audio.VolumeChanged -= OnServiceVolumeChanged;
        _output.Stop();
        _output.Dispose();
        _reader?.Dispose();
        _reader = null;
    }

    partial void OnVolumeChanged(float value)
    {
        if (!_disposed)
            _output.Volume = value;
    }

    private void OnServiceVolumeChanged()
    {
        if (!_disposed)
            Volume = _audio.Volume;
    }

    private void OnOutputDeviceChanged()
    {
        if (_disposed) return;

        var wasPlaying = _output.PlaybackState == PlaybackState.Playing;
        var position = CurrentTime;

        _output.Stop();
        _output.Dispose();
        _output = _audio.CreateOutputDevice();
        _output.Volume = Volume;

        if (_reader is null) return;

        _reader.CurrentTime = position;
        _output.Init(_reader);

        if (wasPlaying)
            _output.Play();
    }
}
