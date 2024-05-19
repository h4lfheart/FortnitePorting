using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.Utils;
using FortnitePorting.OpenGL.OpenTK;
using FortnitePorting.OpenGL.Rendering;
using FortnitePorting.OpenGL.Rendering.Levels;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using Serilog;

namespace FortnitePorting.OpenGL;

public class ModelViewerTkOpenGlControl : BaseTkOpenGlControl
{
    public static ModelViewerTkOpenGlControl Instance;
    public Camera Camera;
    public RenderManager Renderer;

    public UObject? QueuedObject; // todo fix this scuffed impl, figure out how to call mesh change on opengl thread?

    public ModelViewerTkOpenGlControl(UObject initialObject)
    {
        QueuedObject = initialObject;
    }
    
    protected override void OpenTkInit()
    {
        base.OpenTkInit();

        Instance = this;

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

    protected override void OpenTkRender()
    {
        base.OpenTkRender();
        
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Update();
        
        Renderer.Render(Camera);
    }

    private void Update()
    {
        if (QueuedObject is not null)
        {
            Renderer.Clear();
            Renderer.Add(QueuedObject);
            QueuedObject = null;

            if (Renderer.Objects.FirstOrDefault() is Level level && level.Actors.FirstOrDefault() is { } actor)
            {
                Camera.Position = actor.Transform.ExtractTranslation();
            }
        }
        
        Camera.AspectRatio = (float) (Bounds.Width / Bounds.Height);
        
        if (!MouseDown) return;
        
        var speed = 0.1f * Camera.Speed;
        if (KeyboardState.IsKeyDown(Key.W))
            Camera.Position += Camera.Direction * speed;
        if (KeyboardState.IsKeyDown(Key.S))
            Camera.Position -= Camera.Direction * speed;
        if (KeyboardState.IsKeyDown(Key.A))
            Camera.Position -= Vector3.Normalize(Vector3.Cross(Camera.Direction, Camera.Up)) * speed;
        if (KeyboardState.IsKeyDown(Key.D))
            Camera.Position += Vector3.Normalize(Vector3.Cross(Camera.Direction, Camera.Up)) * speed;
        if (KeyboardState.IsKeyDown(Key.E))
            Camera.Position += Camera.Up * speed;
        if (KeyboardState.IsKeyDown(Key.Q))
            Camera.Position -= Camera.Up * speed;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        
        Camera.Speed += (float) e.Delta.Y;
        Camera.Speed = Camera.Speed.Clamp(0.25f, 20.0f);
    }

    private bool MouseDown;
    private Point LastPointerPosition;
    
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var point = e.GetCurrentPoint(this);

        var deltaX = point.Position.X - LastPointerPosition.X;
        var deltaY = point.Position.Y - LastPointerPosition.Y;

        LastPointerPosition = point.Position;
        MouseDown = point.Properties.IsLeftButtonPressed;
        
        if (MouseDown)
        {
            Camera.CalculateDirection((float) (deltaX * Camera.Sensitivity), (float) (deltaY * Camera.Sensitivity));
            //Cursor = MouseCursor.Empty;
            //CursorState = CursorState.Grabbed;
        }
        else
        {
            //Cursor = MouseCursor.Default;
            //CursorState = CursorState.Normal;
        }
    }

}