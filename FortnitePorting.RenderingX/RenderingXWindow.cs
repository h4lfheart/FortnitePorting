using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FortnitePorting.RenderingX;

public class RenderingXWindow : GameWindow
{
    private static readonly GameWindowSettings GameSettings = new()
    {
        UpdateFrequency = 60
    };
    
    private static readonly NativeWindowSettings NativeSettings = new()
    {
        ClientSize = new Vector2i(1280, 720),
        WindowBorder = WindowBorder.Resizable,
        Profile = ContextProfile.Core,
        Vsync = VSyncMode.Adaptive,
        APIVersion = new Version(4, 6),
        StartVisible = true,
        Title = "FortnitePorting.RenderingX",
        NumberOfSamples = 4
    };
    
    
    public RenderingXWindow() : base(GameSettings, NativeSettings)
    {
        
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        IsVisible = true;
    }
    

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.ClearColor(36f / 255f, 36f / 255f, 36f / 255f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        
        SwapBuffers();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
    }
}