using Avalonia.Controls;

namespace FortnitePorting.Shared.Framework;

public abstract class WindowBase<T> : Window where T : ViewModelBase, new()
{
    protected readonly T ViewModel;

    public WindowBase(bool initializeViewModel = true)
    {
        ViewModel = ViewModelRegistry.Register<T>();
        
        if (initializeViewModel)
        {
            Task.Run(async () => await ViewModel.Initialize());
        }
    }
}