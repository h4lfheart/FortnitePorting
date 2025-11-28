using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace FortnitePorting.Services;

// TODO actually make this part of the service collection
public static class TaskService
{
    public static event ExceptionDelegate Exception; 
    public delegate void ExceptionDelegate(Exception exception);
    
    public static Task Run(Func<Task> function)
    {
        return Task.Run(async () =>
        {
            try
            {
                await function().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Exception?.Invoke(e);
            }
        });
    }

    public static async Task RunAsync(Func<Task> function)
    {
        await Task.Run(async () =>
        {
            try
            {
                await function().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Exception?.Invoke(e);
            }
        });
    }

    public static Task Run(Action function)
    {
        return Task.Run(() =>
        {
            try
            {
                function();
            }
            catch (Exception e)
            {
                Exception?.Invoke(e);
            }
        });
    }

    public static async Task RunAsync(Action function)
    {
        await Task.Run(() =>
        {
            try
            {
                function();
            }
            catch (Exception e)
            {
                Exception?.Invoke(e);
            }
        });
    }

    public static void RunDispatcher(Func<Task> function, DispatcherPriority priority = default)
    {
        Dispatcher.UIThread.Invoke(async () =>
        {
            try
            {
                await function();
            }
            catch (Exception e)
            {
                Exception?.Invoke(e);
            }
        }, priority);
    }

    public static async Task RunDispatcherAsync(Func<Task> function, DispatcherPriority priority = default)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                await function();
            }
            catch (Exception e)
            {
                Exception?.Invoke(e);
            }
        }, priority);
    }
    
    public static void RunDispatcher(Action function, DispatcherPriority priority = default)
    {
        try
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                try
                {
                    function();
                }
                catch (Exception e)
                {
                    Exception?.Invoke(e);
                }
            }, priority);
        }
        catch (Exception e)
        {
            Exception?.Invoke(e);
        }
    }

    public static async Task RunDispatcherAsync(Action function, DispatcherPriority priority = default)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                try
                {
                    function();
                }
                catch (Exception e)
                {
                    Exception?.Invoke(e);
                }
            }, priority);
        }
        catch (Exception e)
        {
            Exception?.Invoke(e);
        }
    }

    extension(Task task)
    {
        public void RunAsynchronously()
        {
            task.Start();
        }
    }
    
}