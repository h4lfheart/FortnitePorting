using System.Collections.Concurrent;
using FortnitePorting.Rendering.Core;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Common.Input;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Serilog;

namespace FortnitePorting.Rendering;

public class RenderingXContext : GameWindow
{
    private Scene _scene;

    private Thread? _renderThread;

    private bool _isRunning;
    private bool _embeddedStarted;
    private volatile bool _isPaused;
    
    private bool _sizeChanged;
    private int _width;
    private int _height;

    private bool _isLooking;
    private bool _skipNextMouseDelta;
    
    private readonly ConcurrentQueue<Action> _commandQueue = new();
    
    private static readonly NativeWindowSettings NativeSettings = new()
    {
        Title = "Fortnite Porting RenderingX",
        ClientSize = new Vector2i(1280, 720),
        APIVersion = new Version(4, 6),
        StartVisible = false,
        WindowBorder = WindowBorder.Hidden,
        NumberOfSamples = 16
    };

    public RenderingXContext(Scene scene) : base(GameWindowSettings.Default, NativeSettings)
    {
        _scene = scene;
        
        MouseMove += delegate(MouseMoveEventArgs args)
        {
            if (_scene.ActiveCamera is null) return;

            var isLooking = MouseState[MouseButton.Left] || MouseState[MouseButton.Right];
            if (isLooking)
            {
                if (!_isLooking)
                {
                    Cursor = MouseCursor.Empty;
                    CursorState = CursorState.Grabbed;
                    _isLooking = true;
                    _skipNextMouseDelta = true;
                    return;
                }

                if (_skipNextMouseDelta)
                {
                    _skipNextMouseDelta = false;
                    return;
                }

                var delta = args.Delta * _scene.ActiveCamera.Sensitivity;
                _scene.ActiveCamera.UpdateDirection(delta.X, delta.Y);
            }
            else if (_isLooking)
            {
                Cursor = MouseCursor.Default;
                CursorState = CursorState.Normal;
                _isLooking = false;
                _skipNextMouseDelta = false;
            }
        };

        MouseWheel += delegate(MouseWheelEventArgs args)
        {
            _scene.ActiveCamera?.Speed = Math.Clamp(_scene.ActiveCamera.Speed + args.OffsetY * 2, 3.0f, 150.0f);
        };

        Resize += delegate(ResizeEventArgs args)
        {
            _sizeChanged = true;
            _width = args.Width;
            _height = args.Height;
            
            _scene.ActiveCamera?.AspectRatio = (float) args.Width / args.Height;
        };
    }
    
    public void EnqueueCommand(Action command)
    {
        _commandQueue.Enqueue(command);
    }

    public void StartEmbedded()
    {
        if (_embeddedStarted) return;
        _embeddedStarted = true;
        _isPaused = true;

        Context?.MakeCurrent();
        OnLoad();
        OnResize(new ResizeEventArgs(ClientSize));
    }

    public unsafe void ProcessEvents()
    {
        if (!_embeddedStarted || WindowPtr == null) return;

        NewInputFrame();
        ProcessWindowEvents(waitForEvents: false);
    }

    public void StopEmbedded()
    {
        if (!_embeddedStarted) return;

        _isRunning = false;
        _renderThread?.Join();
        _embeddedStarted = false;
    }

    public void Pause() => _isPaused = true;

    public void Resume() => _isPaused = false;
    
    private void ProcessCommands()
    {
        while (_commandQueue.TryDequeue(out var command))
        {
            try
            {
                command();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        
    }

    private void RenderLoop()
    {
        MakeCurrent();

        _isRunning = true;
        
        VSync = VSyncMode.On;
        
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        IsVisible = true;
        
        var lastFrameTime = GLFW.GetTime();
        while (_isRunning)
        {
            if (_isPaused)
            {
                ProcessCommands();
                Thread.Sleep(50);
                lastFrameTime = GLFW.GetTime();
                continue;
            }

            var currentTime = GLFW.GetTime();
            var deltaTime = (float) (currentTime - lastFrameTime);
            lastFrameTime = currentTime;
            
            ProcessCommands();
            Update(deltaTime);
            Render();
        }
        
        _scene.Destroy();
        
        Context.MakeNoneCurrent();
    }

    private void Update(float deltaTime)
    {
        _scene.Update(deltaTime);

        _scene.ActiveCamera?.UpdateMovement(KeyboardState, deltaTime);
        
        if (_sizeChanged)
        {
            GL.Viewport(0, 0, _width, _height);
            _sizeChanged = false;
        }
    }
    
    private void Render()
    {
        GL.ClearColor(36f / 255f, 36f / 255f, 36f / 255f, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
        
        _scene.Render();
        
        SwapBuffers();
    }
    
    protected override void OnLoad()
    {
        Context.MakeNoneCurrent();

        _renderThread = new Thread(RenderLoop) { IsBackground = true };
        _renderThread.Start();
    }

    protected override void OnUnload()
    {
        _isRunning = false;
        _renderThread?.Join();
        Dispose();
    }

}