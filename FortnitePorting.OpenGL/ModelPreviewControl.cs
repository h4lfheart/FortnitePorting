using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Serilog;

namespace FortnitePorting.OpenGL;

public class ModelPreviewControl : NativeControlHost
{
    public ModelViewerContext Context;
    public PlatformHandle Handle;
    
    public ModelPreviewControl()
    {
        Context = new ModelViewerContext();
    }
    
    protected override unsafe IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        Handle = new PlatformHandle(GLFW.GetWin32Window(Context.WindowPtr), "OpenTKWindow");
        return Handle;
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        Context.Run();
    }
}