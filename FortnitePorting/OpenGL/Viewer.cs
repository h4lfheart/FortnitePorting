using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.Utils;
using FortnitePorting.OpenGL.Renderable;
using FortnitePorting.Views.Controls;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;
using WindowState = OpenTK.Windowing.Common.WindowState;

namespace FortnitePorting.OpenGL;

public class Viewer : GameWindow
{
    public Camera Cam;
    public Renderer Renderer;

    private bool IsFullscreen;

    public Viewer(GameWindowSettings gwSettings, NativeWindowSettings nwSettings) : base(gwSettings, nwSettings)
    {
        Cam = new Camera();
        Renderer = new Renderer();
        Renderer.Setup();

        LoadIcon();
        CenterWindow();
    }

    public void LoadMeshAssets(List<IExportableAsset> exportables)
    {
        Title = exportables.Count == 1 ? $"Mesh Viewer - {exportables[0].DisplayName}" : "Mesh Viewer - Multiple Assets";
        Renderer.Clear();

        var x = 0.0f;
        foreach (var exportable in exportables)
        {
            var transform = Matrix4.CreateTranslation(x / 50, 0, 0);
            switch (exportable.Asset)
            {
                case USkeletalMesh skeletalMesh:
                    var sk = new UnrealMesh(skeletalMesh, transform);
                    Renderer.AddDynamic(sk);
                    break;
                case UStaticMesh staticMesh:
                    var sm = new UnrealMesh(staticMesh, transform);
                    Renderer.AddDynamic(sm);
                    break;
            }

            x += 128;
        }
    }

    public void LoadMeshAsset(IExportableAsset exportable, Matrix4? transform = null)
    {
        Title = $"Mesh Viewer - {exportable.DisplayName}";
        Renderer.Clear();

        switch (exportable.Asset)
        {
            case USkeletalMesh skeletalMesh:
                Renderer.AddDynamic(new UnrealMesh(skeletalMesh, transform ?? Matrix4.Zero));
                break;
            case UStaticMesh staticMesh:
                Renderer.AddDynamic(new UnrealMesh(staticMesh, transform ?? Matrix4.Zero));
                break;
        }
    }

    public void LoadAsset(UStaticMesh staticMesh, Matrix4? transform = null)
    {
        Renderer.AddDynamic(new UnrealMesh(staticMesh, transform ?? Matrix4.Zero));
    }

    public void LoadAsset(USkeletalMesh skeletalMesh, Matrix4? transform = null)
    {
        Renderer.AddDynamic(new UnrealMesh(skeletalMesh, transform ?? Matrix4.Zero));
    }

    protected override void OnKeyDown(KeyboardKeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Keys.F11:
                ToggleFullscreen();
                break;
            case Keys.Escape:
                Close();
                break;
        }
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.Enable(EnableCap.FramebufferSrgb);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

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
        Cam.Speed = Cam.Speed.Clamp(0.25f, 20.0f);
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

    private void ToggleFullscreen()
    {
        if (IsFullscreen)
        {
            WindowBorder = WindowBorder.Resizable;
            WindowState = WindowState.Normal;
            Size = new Vector2i(960, 540);
        }
        else
        {
            WindowBorder = WindowBorder.Hidden;
            WindowState = WindowState.Fullscreen;
        }

        IsFullscreen = !IsFullscreen;
    }
}