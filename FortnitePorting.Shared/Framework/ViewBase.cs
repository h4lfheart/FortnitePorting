using Avalonia.Controls;

namespace FortnitePorting.Shared.Framework;

public abstract class ViewBase<T> : UserControl where T : ViewModelBase, new()
{
    protected readonly T ViewModel;

    public ViewBase(bool initializeViewModel = true)
    {
        ViewModel = ViewModelRegistry.Register<T>();
        DataContext = ViewModel;

        if (initializeViewModel)
        {
            Task.Run(async () => await ViewModel.Initialize());
        }
    }
}