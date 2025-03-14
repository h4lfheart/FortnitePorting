using Avalonia.Controls;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.Shared.Framework;

public abstract class WindowBase<T> : Window where T : ViewModelBase, new()
{
    protected readonly T WindowModel;

    public WindowBase(ViewModelBase? templateWindowModel = null, bool initializeWindowModel = true)
    {
        WindowModel = templateWindowModel is not null ? ViewModelRegistry.Register<T>(templateWindowModel) : ViewModelRegistry.New<T>();

        if (initializeWindowModel)
        {
            TaskService.Run(WindowModel.Initialize);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        ViewModelRegistry.Unregister<T>();
    }
}

public static class WindowExtensions 
{
    public static void BringToTop(this Window window)
    {
        window.Topmost = true;
        window.Topmost = false;
    }
}