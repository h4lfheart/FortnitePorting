using System;
using System.Threading.Tasks;
using FortnitePorting.Application;

namespace FortnitePorting.Services;

public static class TaskService
{
    public static void Run(Func<Task?> function)
    {
        Task.Run(async () =>
        {
            try
            {
                await function().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                App.HandleException(e);
            }
        });
    }
    
    public static async Task RunAsync(Func<Task?> function)
    {
        await Task.Run(async () =>
        {
            try
            {
                await function().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                App.HandleException(e);
            }
        });
    }
}