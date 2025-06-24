using CUE4Parse.Utils;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Core;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FortnitePorting.RenderingX;

public class ViewerWindow : GameWindow
{
    public Scene Scene;
    
    private static readonly GameWindowSettings GameSettings = new()
    {
        UpdateFrequency = 144
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
    
    
    public ViewerWindow(Scene scene) : base(GameSettings, NativeSettings)
    {
        Scene = scene;
        
        MouseMove += delegate(MouseMoveEventArgs args)
        {
            var delta = args.Delta * Scene.ActiveCamera.Sensitivity;
            if (MouseState[MouseButton.Left] || MouseState[MouseButton.Right])
            {
                Scene.ActiveCamera.UpdateDirection(delta.X, delta.Y);
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
            Scene.ActiveCamera.Speed += args.OffsetY * 0.01f;
            Scene.ActiveCamera.Speed = Scene.ActiveCamera.Speed.Clamp(0.001f, 20.0f);
        };
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

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        Scene.Update((float) args.Time);

        var transform = Scene.ActiveCamera.Owner.GetComponent<TransformComponent>()!;
        if (KeyboardState.IsKeyDown(Keys.W))
            transform.LocalPosition += Scene.ActiveCamera.Direction * Scene.ActiveCamera.Speed;
        if (KeyboardState.IsKeyDown(Keys.S))
            transform.LocalPosition -= Scene.ActiveCamera.Direction * Scene.ActiveCamera.Speed;
        if (KeyboardState.IsKeyDown(Keys.A))
            transform.LocalPosition -= Vector3.Normalize(Vector3.Cross(Scene.ActiveCamera.Direction, Scene.ActiveCamera.Up)) * Scene.ActiveCamera.Speed;
        if (KeyboardState.IsKeyDown(Keys.D))
            transform.LocalPosition += Vector3.Normalize(Vector3.Cross(Scene.ActiveCamera.Direction, Scene.ActiveCamera.Up)) * Scene.ActiveCamera.Speed;
        if (KeyboardState.IsKeyDown(Keys.E))
            transform.LocalPosition += Scene.ActiveCamera.Up * Scene.ActiveCamera.Speed;
        if (KeyboardState.IsKeyDown(Keys.Q))
            transform.LocalPosition -= Scene.ActiveCamera.Up * Scene.ActiveCamera.Speed;
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.ClearColor(36f / 255f, 36f / 255f, 36f / 255f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        
        Scene.Render();
        
        SwapBuffers();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
    }
}