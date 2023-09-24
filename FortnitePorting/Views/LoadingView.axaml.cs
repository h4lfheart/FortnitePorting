using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;
using Serilog;

namespace FortnitePorting.Views;

public partial class LoadingView : ViewBase<LoadingViewModel>
{
    public LoadingView()
    {
        InitializeComponent();
        
        TaskService.Run(async () =>
        {
            await ViewModelRegistry.Register<CUE4ParseViewModel>().Initialize();
            await Dispatcher.UIThread.InvokeAsync(() => MainVM.SetAssetView<AssetsView>());
        });
       
    }
}