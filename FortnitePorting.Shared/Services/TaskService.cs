using Avalonia.Threading;

namespace FortnitePorting.Shared.Services;

public static class TaskService
{
    public static void Run(Func<Task> function)
    {
        Task.Run(async () => await function().ConfigureAwait(false));
    }

    public static async Task RunAsync(Func<Task> function)
    {
        await Task.Run(async () => await function().ConfigureAwait(false));
    }

    public static void Run(Action function)
    {
        Task.Run(function);
    }

    public static async Task RunAsync(Action function)
    {
        await Task.Run(function);
    }

    public static void RunDispatcher(Func<Task> function, DispatcherPriority priority = default)
    {
        Dispatcher.UIThread.Invoke(async () => await function(), priority);
    }

    public static async Task RunDispatcherAsync(Func<Task> function, DispatcherPriority priority = default)
    {
        await Dispatcher.UIThread.InvokeAsync(async () => await function(), priority);
    }
    
    public static void RunDispatcher(Action function, DispatcherPriority priority = default)
    {
        Dispatcher.UIThread.Invoke(function, priority);
    }

    public static async Task RunDispatcherAsync(Action function, DispatcherPriority priority = default)
    {
        await Dispatcher.UIThread.InvokeAsync(function, priority);
    }
}