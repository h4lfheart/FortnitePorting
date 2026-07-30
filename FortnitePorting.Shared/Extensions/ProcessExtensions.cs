using System.Diagnostics;

namespace FortnitePorting.Shared.Extensions;

public static class ProcessExtensions
{
    public static bool IsProcessRunning(string processPath)
    {
        var processName = Path.GetFileNameWithoutExtension(processPath);
        var processes = Process.GetProcessesByName(processName);
        return processes.Any(process =>
            process.MainModule?.FileName.StartsWith(processPath, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public static Process? GetRunningProcess(string processPath)
    {
        var processName = Path.GetFileNameWithoutExtension(processPath);
        var processes = Process.GetProcessesByName(processName);
        return processes.FirstOrDefault(process =>
            process.MainModule?.FileName.StartsWith(processPath, StringComparison.OrdinalIgnoreCase) ?? false);
    }
}
