using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Installer.Views;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;

namespace FortnitePorting.Installer.WindowModels;

public partial class AppWindowModel : ViewModelBase
{
    [ObservableProperty] private UserControl _activeView;

    public override async Task Initialize()
    {
        SetView<IntroView>();
    }

    public void SetView<T>() where T : UserControl, new()
    {
        TaskService.RunDispatcher(() => ActiveView = new T());
    }
}