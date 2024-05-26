using System.Buffers;
using System.ComponentModel;
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

namespace FortnitePorting.OpenGL;

public class ModelViewerContext : GameWindow
{
    public Thread RenderThread;
    public bool Exit = false;
    public bool SizeChanged;
    public int Width;
    public int Height;

    public UObject? QueuedObject;

    public RenderManager Renderer;
    public Camera Camera = new();
    
    private static readonly NativeWindowSettings Settings = new()
    {
        ClientSize = new Vector2i(960, 540),
        APIVersion = new Version(4, 6),
        NumberOfSamples = 32,
        WindowBorder = WindowBorder.Hidden,
        StartVisible = false
    };
    
    public ModelViewerContext() : base(GameWindowSettings.Default, Settings)
    {
        MouseMove += delegate(MouseMoveEventArgs args)
        {
            var delta = args.Delta * Camera.Sensitivity;
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
        };

        MouseWheel += delegate(MouseWheelEventArgs args)
        {
            Camera.Speed += args.OffsetY * 0.01f;
            Camera.Speed = Camera.Speed.Clamp(0.01f, 20.0f);
        };

        Resize += delegate(ResizeEventArgs args)
        {
            SizeChanged = true;
            Width = args.Width;
            Height = args.Height;
        };
    }
    

    public void RenderLoop()
    {
        MakeCurrent();

        Renderer = new RenderManager();
        Renderer.Setup();
        
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        while (!Exit)
        {
            Update();
            Render();
        }
        
        Renderer.Dispose();
        
        Context.MakeNoneCurrent();
    }

    private void Update()
    {
        if (QueuedObject is not null)
        {
            Renderer.Add(QueuedObject);
            QueuedObject = null;

            if (Renderer.Objects.LastOrDefault() is Level level)
            {
                var averagePosition = level.Actors.Aggregate(Vector3.Zero, (pos, actor) => pos + actor.Transform.ExtractTranslation()) / level.Actors.Count;
                Camera.Position = averagePosition + new Vector3(0, 50, 0);
            }
        }
        
        if (SizeChanged)
        {
            GL.Viewport(0, 0, Width, Height);
            Camera.AspectRatio = (float) Width / Height;
            SizeChanged = false;
        }
        
        if (KeyboardState.IsKeyDown(Keys.W))
            Camera.Position += Camera.Direction * Camera.Speed;
        if (KeyboardState.IsKeyDown(Keys.S))
            Camera.Position -= Camera.Direction * Camera.Speed;
        if (KeyboardState.IsKeyDown(Keys.A))
            Camera.Position -= Vector3.Normalize(Vector3.Cross(Camera.Direction, Camera.Up)) * Camera.Speed;
        if (KeyboardState.IsKeyDown(Keys.D))
            Camera.Position += Vector3.Normalize(Vector3.Cross(Camera.Direction, Camera.Up)) * Camera.Speed;
        if (KeyboardState.IsKeyDown(Keys.E))
            Camera.Position += Camera.Up * Camera.Speed;
        if (KeyboardState.IsKeyDown(Keys.Q))
            Camera.Position -= Camera.Up * Camera.Speed;
    }
    
    private void Render()
    {
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        Renderer.Render(Camera);
        
        SwapBuffers();
    }

    protected override void OnLoad()
    {
        Context.MakeNoneCurrent();

        RenderThread = new Thread(RenderLoop) { IsBackground = true };
        RenderThread.Start();
    }

    protected override void OnUnload()
    {
        Exit = true;
        RenderThread.Join();
        Dispose();
    }
}