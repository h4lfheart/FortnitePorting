using System.Buffers;
using System.ComponentModel;
using Avalonia.Platform;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.Utils;
using FortnitePorting.OpenGL.Rendering;
using FortnitePorting.OpenGL.Rendering.Levels;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.PixelFormats;
using SkiaSharp;
using Image = OpenTK.Windowing.Common.Input.Image;

namespace FortnitePorting.OpenGL;

public class ModelViewerWindow : GameWindow
{
    public static ModelViewerWindow? Instance;
    public Camera Camera;
    public RenderManager Renderer;

    public UObject? QueuedObject; // todo fix this scuffed impl, figure out how to call mesh change on opengl thread?
    
    public ModelViewerWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings) : base(gameWindowSettings, nativeWindowSettings)
    {
        Instance = this;
        
        LoadIcon();
    }

    public void LoadIcon()
    {
        var stream = AssetLoader.Open(new Uri("avares://FortnitePorting/Assets/LogoV3.png"));
        var image = SixLabors.ImageSharp.Image.Load(stream).CloneAs<Rgba32>();

        var bytes = new byte[image.Width * image.Height * 4];
        image.CopyPixelDataTo(bytes);

        Icon = new WindowIcon(new Image(image.Width, image.Height, bytes));
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        
        Camera = new Camera();
        
        Renderer = new RenderManager();
        Renderer.Setup();
        
        Renderer.Clear();
        Renderer.Add(QueuedObject);
        QueuedObject = null;
        if (Renderer.Objects.FirstOrDefault() is Level level && level.Actors.FirstOrDefault() is { } actor)
        {
            Camera.Position = actor.Transform.ExtractTranslation();
        }
        
        GL.ClearColor(Color4.Black);
        
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        Renderer.Render(Camera);
        
        SwapBuffers();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        if (QueuedObject is not null)
        {
            //Renderer.Clear();
            Renderer.Add(QueuedObject);
            QueuedObject = null;

            if (Renderer.Objects.LastOrDefault() is Level level && level.Actors.FirstOrDefault() is { } actor)
            {
                Camera.Position = actor.Transform.ExtractTranslation();
            }
        }
        
        var speed = 0.1f * Camera.Speed;
        if (KeyboardState.IsKeyDown(Keys.W))
            Camera.Position += Camera.Direction * speed;
        if (KeyboardState.IsKeyDown(Keys.S))
            Camera.Position -= Camera.Direction * speed;
        if (KeyboardState.IsKeyDown(Keys.A))
            Camera.Position -= Vector3.Normalize(Vector3.Cross(Camera.Direction, Camera.Up)) * speed;
        if (KeyboardState.IsKeyDown(Keys.D))
            Camera.Position += Vector3.Normalize(Vector3.Cross(Camera.Direction, Camera.Up)) * speed;
        if (KeyboardState.IsKeyDown(Keys.E))
            Camera.Position += Camera.Up * speed;
        if (KeyboardState.IsKeyDown(Keys.Q))
            Camera.Position -= Camera.Up * speed;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        
        Camera.Speed += e.OffsetY;
        Camera.Speed = Camera.Speed.Clamp(0.25f, 20.0f);
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        
        var delta = e.Delta * Camera.Sensitivity;
        if (MouseState[MouseButton.Left] || MouseState[MouseButton.Right])
        {
            Camera.CalculateDirection(delta.X, delta.Y);
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
        Camera.AspectRatio = (float) e.Width / e.Height;
    }
    
    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Keys.Escape:
                Close();
                break;
        }
    }
    
    protected override unsafe void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);

        Instance = null;
        GLFW.DestroyWindow(WindowPtr);
    }
}