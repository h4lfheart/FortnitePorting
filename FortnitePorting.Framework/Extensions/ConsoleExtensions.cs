using System.Runtime.InteropServices;

namespace FortnitePorting.Framework.Extensions;

public static class ConsoleExtensions
{
    private static bool IsAllocated;
    
    [DllImport("kernel32")]
    public static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    public static void AllocateConsole(string title = "Fortnite Porting Console")
    {
        IsAllocated = true;
        AllocConsole();
        Console.Title = title;
    }

    public static void ToggleConsole(bool show)
    {
        if (!IsAllocated)
        {
            AllocateConsole();
        }
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        var handle = GetConsoleWindow();
        ShowWindow(handle, show ? SW_SHOW : SW_HIDE);
    }
}