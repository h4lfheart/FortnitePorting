using Avalonia.Controls;
using FortnitePorting.Services;

namespace FortnitePorting.Framework;

public class ViewBase<T> : UserControl where T : ViewModelBase, new()
{
    protected readonly T ViewModel;

    public ViewBase(T? custom = null, bool lateInit = false)
    {
        ViewModel = custom ?? ViewModelRegistry.Register<T>();
        DataContext = ViewModel;

        if (!lateInit) TaskService.Run(async () => await ViewModel.Initialize());
    }
}