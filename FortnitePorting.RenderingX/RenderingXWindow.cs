using System.ComponentModel;
using FortnitePorting.RenderingX.Components;
using FortnitePorting.RenderingX.Core;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FortnitePorting.RenderingX;

public class RenderingXWindow(Scene _scene) : GameWindow(GameSettings, NativeSettings)
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

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        
        var delta = e.Delta * _scene.ActiveCamera.Sensitivity;
        if (MouseState[MouseButton.Left] || MouseState[MouseButton.Right])
        {
            _scene.ActiveCamera.UpdateDirection(delta.X, delta.Y);
            Cursor = MouseCursor.Empty;
            CursorState = CursorState.Grabbed;
        }
        else
        {
            Cursor = MouseCursor.Default;
            CursorState = CursorState.Normal;
        }
    }
    
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        
        _scene.ActiveCamera.Speed = Math.Clamp(_scene.ActiveCamera.Speed + (e.OffsetY * 0.01f), 0.01f, 20.0f);
    }
    
    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        
        _scene.Update((float) args.Time);

        if (_scene.ActiveCamera.Actor?.GetComponent<SpatialComponent>() is not { } transform)
            return;
        
        if (KeyboardState.IsKeyDown(Keys.W))
            transform.LocalPosition += _scene.ActiveCamera.Direction * _scene.ActiveCamera.Speed;
        if (KeyboardState.IsKeyDown(Keys.S))
            transform.LocalPosition -= _scene.ActiveCamera.Direction * _scene.ActiveCamera.Speed;
        if (KeyboardState.IsKeyDown(Keys.A))
            transform.LocalPosition -= Vector3.Normalize(Vector3.Cross(_scene.ActiveCamera.Direction, _scene.ActiveCamera.Up)) * _scene.ActiveCamera.Speed;
        if (KeyboardState.IsKeyDown(Keys.D))
            transform.LocalPosition += Vector3.Normalize(Vector3.Cross(_scene.ActiveCamera.Direction, _scene.ActiveCamera.Up)) * _scene.ActiveCamera.Speed;
        if (KeyboardState.IsKeyDown(Keys.E))
            transform.LocalPosition += _scene.ActiveCamera.Up * _scene.ActiveCamera.Speed;
        if (KeyboardState.IsKeyDown(Keys.Q))
            transform.LocalPosition -= _scene.ActiveCamera.Up * _scene.ActiveCamera.Speed;
    }


    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        
        GL.ClearColor(36f / 255f, 36f / 255f, 36f / 255f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        
        _scene.Render();
        
        SwapBuffers();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);
        
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        
        _scene.Destroy();
    }
}