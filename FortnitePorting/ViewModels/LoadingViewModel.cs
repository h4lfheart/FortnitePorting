using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.AppUtils;
using FortnitePorting.Services;
using FortnitePorting.Views;
using Ionic.Zip;

namespace FortnitePorting.ViewModels;

public partial class LoadingViewModel : ObservableObject
{
    [ObservableProperty]
    private string titleText = "Fortnite Porting";

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
            await LoadVGMStream();
            AppVM.CUE4ParseVM = new CUE4ParseViewModel(AppSettings.Current.ArchivePath, AppSettings.Current.InstallType);
            await AppVM.CUE4ParseVM.Initialize();

            Application.Current.Dispatcher.Invoke(() =>
            {
                AppHelper.OpenWindow<MainView>();
                AppHelper.CloseWindow<LoadingView>();
            });
        });
    }

    private async Task LoadVGMStream()
    {
        var path = Path.Combine(App.VGMStreamFolder.FullName, "vgmstream-win.zip");
        if (File.Exists(path)) return;

        var file = await EndpointService.DownloadFileAsync("https://github.com/vgmstream/vgmstream/releases/latest/download/vgmstream-win.zip", path);
        if (!file.Exists) return;
        if (file.Length <= 0) return;

        var zip = ZipFile.Read(file.FullName);
        foreach (var zipFile in zip)
        {
            zipFile.Extract(App.VGMStreamFolder.FullName, ExtractExistingFileAction.OverwriteSilently);
        }
    }
}