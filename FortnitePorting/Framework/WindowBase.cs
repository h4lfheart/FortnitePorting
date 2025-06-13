using System;
using Avalonia.Controls;
using FortnitePorting.Application;
using FortnitePorting.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FortnitePorting.Framework;

public abstract class WindowBase<T> : Window where T : WindowModelBase
{
    public T WindowModel { get; set; }

    public WindowBase(T? templateWindowModel = null, bool initializeWindowModel = true)
    {
        WindowModel = templateWindowModel ?? AppServices.Services.GetRequiredService<T>();
        WindowModel.Window = this;
        
        if (initializeWindowModel)
        {
            TaskService.Run(WindowModel.Initialize);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        ViewModelRegistry.Unregister<T>();
    }
}

public static class WindowExtensions 
{
    public static void BringToTop(this Window window)
    {
        window.Topmost = true;
        window.Topmost = false;
    }
}