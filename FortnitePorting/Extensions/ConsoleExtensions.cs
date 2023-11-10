using System;
using System.Runtime.InteropServices;

namespace FortnitePorting.Extensions;

public static class ConsoleExtensions
{
#if _WINDOWS
    
    [DllImport("kernel32")]
    public static extern bool AllocConsole();
    
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void ToggleConsole(bool show)
    {
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        var handle = GetConsoleWindow();
        ShowWindow(handle, show ? SW_SHOW : SW_HIDE);
    }
#endif
}