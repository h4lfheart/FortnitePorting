using Avalonia.Controls;
using Avalonia.Interactivity;
using FortnitePorting.Application;
using FortnitePorting.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FortnitePorting.Framework;

public abstract class ViewBase<T> : UserControl where T : ViewModelBase
{
    protected readonly T ViewModel;

    public ViewBase(T? templateViewModel = null)
    {
        ViewModel = templateViewModel ?? AppServices.Services.GetRequiredService<T>();
        DataContext = ViewModel;
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (!ViewModel.IsInitialized)
        {
            ViewModel.IsInitialized = true;
            TaskService.Run(ViewModel.Initialize);
        }

        await ViewModel.OnViewOpened();
    }

    protected override async void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        
        await ViewModel.OnViewExited();
    }
}
