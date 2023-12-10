using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace FortnitePorting.Framework.Extensions;

public static class StopwatchExtensions
{
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ElapsedSeconds(Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.Elapsed.TotalSeconds;
    }
}