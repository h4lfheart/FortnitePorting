using Avalonia.Controls;

namespace FortnitePorting.Framework;

public class ViewBase<T> : UserControl where T : ViewModelBase, new()
{
    protected readonly T ViewModel;

    protected ViewBase()
    {
        ViewModel = ViewModelRegistry.Register<T>();
        DataContext = ViewModel;
    }
}