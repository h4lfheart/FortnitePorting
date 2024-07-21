using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FortnitePorting.Rendering;

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