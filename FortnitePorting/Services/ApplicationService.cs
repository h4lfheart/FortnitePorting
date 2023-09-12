using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Services;

public static class ApplicationService
{
    public static ApplicationViewModel AppVM;
    public static LoadingViewModel LoadingVM => ViewModelRegistry.Get<LoadingViewModel>();
    public static CUE4ParseViewModel CUE4ParseVM => ViewModelRegistry.Get<CUE4ParseViewModel>();
    public static IStorageProvider StorageProvider => ApplicationLifetime!.MainWindow!.StorageProvider;

    private static readonly IClassicDesktopStyleApplicationLifetime? ApplicationLifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    public static void Shutdown()
    {
        ApplicationLifetime?.Shutdown();
    }
}