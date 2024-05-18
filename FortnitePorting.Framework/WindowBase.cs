using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Framework.Services;

namespace FortnitePorting.Framework;

public class WindowBase<T> : Window where T : ViewModelBase, new()
{
    protected readonly T ViewModel;

    public WindowBase(T? viewModel = null, bool initialize = true)
    {
        ViewModel = viewModel ?? ViewModelRegistry.Register<T>();
        DataContext = ViewModel;

        if (initialize)
        {
            TaskService.Run(async () => await ViewModel.Initialize());
        }
    }

    ~WindowBase()
    {
        ViewModelRegistry.Unregister<T>();
    }
}