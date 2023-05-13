using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.AppUtils;
using FortnitePorting.Views;

namespace FortnitePorting.ViewModels;

public partial class LoadingViewModel : ObservableObject
{
    [ObservableProperty]
    private string loadingText = "Starting";

    public void Update(string text)
    {
        LoadingText = text;
    }
    
    public async Task Initialize()
    {
        await Task.Run(async () =>
        {
            AppVM.CUE4ParseVM = new CUE4ParseViewModel(AppSettings.Current.ArchivePath, AppSettings.Current.InstallType);
            await AppVM.CUE4ParseVM.Initialize();
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                AppHelper.OpenWindow<NewMainView>();
                AppHelper.CloseWindow<LoadingView>();
            });
          
        });
    }
}