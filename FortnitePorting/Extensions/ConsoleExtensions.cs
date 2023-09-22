using System.Runtime.InteropServices;

namespace FortnitePorting.Extensions;

public static class ConsoleExtensions
{
#if DEBUG
    [DllImport("kernel32.dll")]
    public static extern bool AttachConsole(int dwProcessId);
#endif
}