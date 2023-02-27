using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Documents;
using CUE4Parse.Utils;
using FortnitePorting.OpenGL.Renderable;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace FortnitePorting.OpenGL;

public class Viewer : GameWindow
{
    private Camera Cam;
    private Renderer Renderer;
    
    public Viewer(GameWindowSettings gwSettings, NativeWindowSettings nwSettings) : base(gwSettings, nwSettings)
    {
        Cam = new Camera();
        Renderer = new Renderer();
        Renderer.AddStatic(new Skybox());
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        CenterWindow();
        LoadIcon();

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.Enable(EnableCap.FramebufferSrgb);

        SetVisibility(true);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        
        SetVisibility(false);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        Renderer.Render(Cam);

        SwapBuffers();
    }
    
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        var speed = (float) args.Time * Cam.Speed;
        if (KeyboardState.IsKeyDown(Keys.W))
            Cam.Position += Cam.Direction * speed;
        if (KeyboardState.IsKeyDown(Keys.S))
            Cam.Position -= Cam.Direction * speed;
        if (KeyboardState.IsKeyDown(Keys.A))
            Cam.Position -= Vector3.Normalize(Vector3.Cross(Cam.Direction, Cam.Up)) * speed;
        if (KeyboardState.IsKeyDown(Keys.D))
            Cam.Position += Vector3.Normalize(Vector3.Cross(Cam.Direction, Cam.Up)) * speed;
        if (KeyboardState.IsKeyDown(Keys.E))
            Cam.Position += Cam.Up * speed;
        if (KeyboardState.IsKeyDown(Keys.Q))
            Cam.Position -= Cam.Up * speed;
    }
    
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        Cam.Speed += e.OffsetY;
        Cam.Speed = Cam.Speed.Clamp(0.25f, 10.0f);
    }
    
    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);

        var delta = e.Delta * Cam.Sensitivity;
        if (MouseState[MouseButton.Right])
        {
            Cam.CalculateDirection(delta.X, delta.Y);
            Cursor = MouseCursor.Empty;
            CursorState = CursorState.Grabbed;
        }
        else
        {
            Cursor = MouseCursor.Default;
            CursorState = CursorState.Normal;
        }
    }
    
    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        Cam.AspectRatio = e.Width / (float) e.Height;
    }
    
    private void LoadIcon()
    {
        var imageResource = Application.GetResourceStream(new Uri("/FortnitePorting;component/FortnitePorting-Dark.png", UriKind.Relative));
        if (imageResource is null) return;
        
        var image = Image.Load<Rgba32>(imageResource.Stream);
        if (image is null) return;

        var imageBytes = new byte[image.Width * image.Height * 32];
        image.CopyPixelDataTo(imageBytes);
        
        Icon = new WindowIcon(new OpenTK.Windowing.Common.Input.Image(image.Width, image.Height, imageBytes));
    }
    
    private unsafe void SetVisibility(bool open)
    {
        GLFW.SetWindowShouldClose(WindowPtr, !open);
        IsVisible = open; 
    }
}