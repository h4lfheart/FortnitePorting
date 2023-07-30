using System;
using System.Runtime.InteropServices;

namespace FortnitePorting.AppUtils;

public static class WindowsUtils
{
    [DllImport("kernel32")]
    public static extern bool AllocConsole();

    [DllImport("kernel32")]
    public static extern bool FreeConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    
    [DllImport("Kernel32")]
    private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

    private delegate bool EventHandler(CtrlType sig);
    static EventHandler _handler;

    public static void InitExitHandler(Func<CtrlType, bool> exitHandler)
    {
        _handler += new EventHandler(exitHandler);
        SetConsoleCtrlHandler(_handler, true);
    }

    public static void ToggleConsole(bool show)
    {
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        var handle = GetConsoleWindow();
        ShowWindow(handle, show ? SW_SHOW : SW_HIDE);
    }
    
    
}

public enum CtrlType
{
    CTRL_C_EVENT = 0,
    CTRL_BREAK_EVENT = 1,
    CTRL_CLOSE_EVENT = 2,
    CTRL_LOGOFF_EVENT = 5,
    CTRL_SHUTDOWN_EVENT = 6
}