using Avalonia.Controls;
using Avalonia.Interactivity;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.Shared.Framework;

public abstract class WindowBase<T> : Window where T : ViewModelBase, new()
{
    protected readonly T WindowModel;

    public WindowBase(bool initializeWindowModel = true)
    {
        WindowModel = ViewModelRegistry.New<T>();

        if (initializeWindowModel)
        {
            TaskService.Run(WindowModel.Initialize);
        }
    }
    
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        await WindowModel.OnViewOpened();
    }

    protected override async void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        
        await WindowModel.OnViewExited();
        ViewModelRegistry.Unregister<T>();
    }


    public void BringToTop()
    {
       Topmost = true;
       Topmost = false;
    }
}