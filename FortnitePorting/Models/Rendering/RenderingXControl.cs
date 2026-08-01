using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using FortnitePorting.Rendering;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FortnitePorting.Models.Rendering;

public class RenderingXControl(RenderingXContext context) : NativeControlHost
{
    private readonly RenderingXContext Context = context;
    private PlatformHandle? Handle;
    private readonly DispatcherTimer _eventTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(16)
    };

    protected override unsafe IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        Handle = new PlatformHandle(GLFW.GetWin32Window(Context.WindowPtr), "RenderingX");
        return Handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        // ignore, owned by context
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        Context.StartEmbedded();
        Context.Resume();

        _eventTimer.Tick += OnEventTick;
        _eventTimer.Start();
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        _eventTimer.Stop();
        _eventTimer.Tick -= OnEventTick;
        Context.Pause();
        base.OnUnloaded(e);
    }

    private void OnEventTick(object? sender, EventArgs e)
    {
        Context.ProcessEvents();
    }
}
