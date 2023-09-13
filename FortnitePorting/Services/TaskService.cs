using System;
using System.Threading.Tasks;
using FortnitePorting.Application;

namespace FortnitePorting.Services;

public static class TaskService
{
    public static void Run(Func<Task?> function)
    {
        try
        {
            Task.Run(function);
        }
        catch (Exception e)
        {
            App.HandleException(e);
        }
    }
    
    public static async Task RunAsync(Func<Task?> function)
    {
        try
        {
            await Task.Run(function).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            App.HandleException(e);
        }
    }
}