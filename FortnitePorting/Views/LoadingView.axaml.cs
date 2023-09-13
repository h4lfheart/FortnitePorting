using System.Threading.Tasks;
using Avalonia.Interactivity;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class LoadingView : ViewBase<LoadingViewModel>
{
    public LoadingView()
    {
        InitializeComponent();
    }

    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        await TaskService.RunAsync(async () => await ViewModelRegistry.Register<CUE4ParseViewModel>().Initialize());
        AppVM.SetView<MainView>();
    }
}