using Avalonia.Controls;

namespace FortnitePorting.Framework;

public class ViewBase<T> : UserControl where T : ViewModelBase, new()
{
    protected readonly T ViewModel;

    public ViewBase(T? custom = null)
    {
        ViewModel = custom ?? ViewModelRegistry.Register<T>();
        DataContext = ViewModel;
    }
}