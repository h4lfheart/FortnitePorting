using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using FortnitePorting.RenderingX;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FortnitePorting.Models.Rendering;

public class RenderingXControl(RenderingXContext context) : NativeControlHost
{
    private RenderingXContext Context = context;
    private PlatformHandle? Handle;

    protected override unsafe IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        Handle = new PlatformHandle(GLFW.GetWin32Window(Context.WindowPtr), "RenderingX");
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