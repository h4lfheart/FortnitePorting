using Avalonia.Controls;
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

    public void BringToTop()
    {
       Topmost = true;
       Topmost = false;
    }
}