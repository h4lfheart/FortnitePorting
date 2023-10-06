using Avalonia.Controls;
using FortnitePorting.Services;

namespace FortnitePorting.Framework;

public class ViewBase<T> : UserControl where T : ViewModelBase, new()
{
    protected readonly T ViewModel;

    public ViewBase(T? custom = null, bool waitInit = false)
    {
        ViewModel = custom ?? ViewModelRegistry.Register<T>();
        DataContext = ViewModel;

        if (!waitInit) TaskService.Run(async () => await ViewModel.Initialize());
    }
}