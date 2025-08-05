using System;
using Avalonia.Controls;
using Avalonia.Input;
using FortnitePorting.Application;
using FortnitePorting.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FortnitePorting.Framework;

public abstract class WindowBase<T> : Window where T : WindowModelBase
{
    public T WindowModel { get; set; }

    public WindowBase(T? templateWindowModel = null, bool initializeWindowModel = true)
    {
        WindowModel = templateWindowModel ?? AppServices.Services.GetService<T>();
        WindowModel.Window = this;
        
        if (initializeWindowModel)
        {
            TaskService.Run(WindowModel.Initialize);
        }
    }

    protected override async void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        
        await WindowModel.OnViewExited();
    }
    
    protected void OnPointerPressedUpperBar(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    protected void OnMinimizePressed(object? sender, PointerPressedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    protected void OnMaximizePressed(object? sender, PointerPressedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }
    
    protected void OnClosePressed(object? sender, PointerPressedEventArgs e)
    {
        Close();
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