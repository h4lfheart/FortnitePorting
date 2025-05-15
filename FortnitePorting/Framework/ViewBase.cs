using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using FortnitePorting.Application;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FortnitePorting.Framework;

public abstract class ViewBase<T> : UserControl where T : ViewModelBase
{
    protected readonly T ViewModel;

    public ViewBase(T? templateViewModel = null, bool initializeViewModel = true)
    {
        ViewModel = templateViewModel ?? AppServices.Services.GetRequiredService<T>();
        DataContext = ViewModel;

        if (initializeViewModel)
        {
            TaskService.Run(ViewModel.Initialize);
        }
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        await ViewModel.OnViewOpened();
    }

    protected override async void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        
        await ViewModel.OnViewExited();
    }
}