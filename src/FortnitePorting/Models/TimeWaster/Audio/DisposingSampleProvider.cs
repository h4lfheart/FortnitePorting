using NAudio.Wave;

namespace FortnitePorting.Models.TimeWaster.Audio;

public class DisposingSampleProvider(AudioFileReader reader) : ISampleProvider
{
    public WaveFormat WaveFormat => reader.WaveFormat;

    private bool _isDisposed;

    public int Read(float[] buffer, int offset, int count)
    {
        if (_isDisposed) return 0;

        var read = reader.Read(buffer, offset, count);
        if (read != 0) return read;
        
        reader.Dispose();
        _isDisposed = true;
        return 0;

    }

}
