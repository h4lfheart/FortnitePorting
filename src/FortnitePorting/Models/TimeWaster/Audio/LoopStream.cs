using NAudio.Wave;

namespace FortnitePorting.Models.TimeWaster.Audio;

public class LoopStream(WaveStream SourceStream) : WaveStream
{
    public override WaveFormat WaveFormat => SourceStream.WaveFormat;
    public override long Length => SourceStream.Length;
    public override long Position
    {
        get => SourceStream.Position;
        set => SourceStream.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var totalBytesRead = 0;

        while (totalBytesRead < count)
        {
            var bytesRead = SourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
            if (bytesRead == 0)
            {
                if (SourceStream.Position == 0) break;
                
                SourceStream.Position = 0;
            }
            totalBytesRead += bytesRead;
        }
        return totalBytesRead;
    }
}