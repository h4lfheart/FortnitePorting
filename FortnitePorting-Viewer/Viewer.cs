using System.Windows;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using SixLabors.ImageSharp.PixelFormats;
using Image = OpenTK.Windowing.Common.Input.Image;

namespace FortnitePorting.Viewer;

public class Viewer : GameWindow
{
    public Viewer(GameWindowSettings gwSettings, NativeWindowSettings nwSettings) : base(gwSettings, nwSettings)
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        CenterWindow();

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.Enable(EnableCap.FramebufferSrgb);
    }
    
    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        SwapBuffers();
    }
}