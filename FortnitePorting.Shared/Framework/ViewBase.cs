using Avalonia.Controls;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.Shared.Framework;

public abstract class ViewBase<T> : UserControl where T : ViewModelBase, new()
{
    protected readonly T ViewModel;

    public ViewBase(ViewModelBase? templateViewModel = null, bool initializeViewModel = true)
    {
        ViewModel = templateViewModel is not null ? ViewModelRegistry.Register<T>(templateViewModel) : ViewModelRegistry.New<T>();
        DataContext = ViewModel;

        if (initializeViewModel)
        {
            TaskService.Run(ViewModel.Initialize);
        }
    }
}