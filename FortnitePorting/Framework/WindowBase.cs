using System;
using Avalonia.Controls;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.Framework;

public abstract class WindowBase<T> : Window where T : WindowModelBase, new()
{
    public T WindowModel { get; set; }

    public WindowBase(WindowModelBase? templateWindowModel = null, bool initializeWindowModel = true)
    {
        WindowModel = templateWindowModel is not null ? ViewModelRegistry.Register<T>(templateWindowModel) : ViewModelRegistry.New<T>();
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