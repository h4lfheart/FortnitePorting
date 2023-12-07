using Avalonia.Controls;
using FortnitePorting.Framework.Services;

namespace FortnitePorting.Framework;

public class ViewBase<T> : UserControl where T : ViewModelBase, new()
{
    protected readonly T ViewModel;

    public ViewBase(T? viewModel = null, bool initialize = true)
    {
        ViewModel = viewModel ?? ViewModelRegistry.Register<T>();
        DataContext = ViewModel;

        if (initialize)
        {
            TaskService.Run(async () => await ViewModel.Initialize());
        }
    }
}