using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using FortnitePorting.Services;
using LibVLCSharp.Shared;

namespace FortnitePorting.Controls.Assets;

public sealed class AssetVideoFrameSource : IDisposable
{
    private const int PictureBufferCount = 3;
    private const uint MaxDisplayWidth = 220;

    private readonly Image _target;
    private readonly Action _onFramePresented;
    private readonly Lock _sync = new();
    private readonly IntPtr[] _buffers = new IntPtr[PictureBufferCount];

    private WriteableBitmap? _bitmap;
    private byte[]? _staging;
    private int _pitch;
    private int _videoWidth;
    private int _videoHeight;
    private int _bufferSize;
    private int _lockIndex;
    private int _framePending;
    private bool _disposed;

    public MediaPlayer.LibVLCVideoFormatCb FormatCallback { get; }
    public MediaPlayer.LibVLCVideoCleanupCb CleanupCallback { get; }
    public MediaPlayer.LibVLCVideoLockCb LockCallback { get; }
    public MediaPlayer.LibVLCVideoDisplayCb DisplayCallback { get; }

    public bool Active { get; set; }

    public AssetVideoFrameSource(Image target, Action onFramePresented)
    {
        _target = target;
        _onFramePresented = onFramePresented;

        FormatCallback = VideoFormat;
        CleanupCallback = Cleanup;
        LockCallback = LockVideo;
        DisplayCallback = Display;
    }

    public void Clear()
    {
        _target.Source = null;
        _bitmap?.Dispose();
        _bitmap = null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Active = false;
        Clear();

        lock (_sync)
        {
            FreeBuffers();
        }
    }

    private uint VideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        Marshal.Copy("RV32"u8.ToArray(), 0, chroma, 4);

        if (width > MaxDisplayWidth && width > 0)
        {
            var scaledHeight = (uint)Math.Round(height * (MaxDisplayWidth / (double)width));
            width = MaxDisplayWidth;
            height = scaledHeight;
        }

        width &= ~1u;
        height &= ~1u;
        if (height == 0) height = 2;

        var pitch = Align(width * 4);
        var alignedLines = Align(height);
        pitches = pitch;
        lines = alignedLines;

        lock (_sync)
        {
            FreeBuffers();

            _videoWidth = (int)width;
            _videoHeight = (int)height;
            _pitch = (int)pitch;
            _bufferSize = (int)(pitch * alignedLines);
            _lockIndex = 0;
            _staging = new byte[_bufferSize];

            for (var i = 0; i < _buffers.Length; i++)
                _buffers[i] = Marshal.AllocHGlobal(_bufferSize);
        }

        return PictureBufferCount;
    }

    private void Cleanup(ref IntPtr opaque)
    {
        lock (_sync)
        {
            FreeBuffers();
        }
    }

    private IntPtr LockVideo(IntPtr opaque, IntPtr planes)
    {
        lock (_sync)
        {
            if (_buffers[0] == IntPtr.Zero) return IntPtr.Zero;

            var index = _lockIndex++ % PictureBufferCount;
            Marshal.WriteIntPtr(planes, _buffers[index]);
            return new IntPtr(index);
        }
    }

    private void Display(IntPtr opaque, IntPtr picture)
    {
        if (!Active || _disposed) return;

        var index = picture.ToInt32();
        if (index < 0 || index >= PictureBufferCount) return;

        lock (_sync)
        {
            var buffer = _buffers[index];
            if (buffer == IntPtr.Zero || _staging is null) return;

            Marshal.Copy(buffer, _staging, 0, _bufferSize);
        }

        if (Interlocked.CompareExchange(ref _framePending, 1, 0) != 0) return;

        TaskService.PostDispatcher(() =>
        {
            try
            {
                PresentFrame();
            }
            finally
            {
                Interlocked.Exchange(ref _framePending, 0);
            }
        }, DispatcherPriority.Render);
    }

    private void PresentFrame()
    {
        if (!Active || _disposed) return;

        byte[]? staging;
        int width;
        int height;
        int pitch;

        lock (_sync)
        {
            if (_staging is null || _videoWidth <= 0 || _videoHeight <= 0) return;

            staging = _staging;
            width = _videoWidth;
            height = _videoHeight;
            pitch = _pitch;
        }

        if (_bitmap is null || _bitmap.PixelSize.Width != width || _bitmap.PixelSize.Height != height)
        {
            _bitmap?.Dispose();
            _bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormats.Bgra8888, AlphaFormat.Opaque);
            _target.Source = _bitmap;
        }

        using (var framebuffer = _bitmap.Lock())
        {
            var destPitch = framebuffer.RowBytes;
            var rowBytes = width * 4;

            unsafe
            {
                fixed (byte* srcBase = staging)
                {
                    var dstBase = (byte*) framebuffer.Address;
                    for (var y = 0; y < height; y++)
                        Buffer.MemoryCopy(srcBase + y * pitch, dstBase + y * destPitch, destPitch, rowBytes);
                }
            }
        }

        _target.InvalidateVisual();
        _onFramePresented();
    }

    private void FreeBuffers()
    {
        for (var i = 0; i < _buffers.Length; i++)
        {
            if (_buffers[i] == IntPtr.Zero) continue;

            Marshal.FreeHGlobal(_buffers[i]);
            _buffers[i] = IntPtr.Zero;
        }
    }

    private static uint Align(uint size) => (size + 31) / 32 * 32;
}
