using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Services;

public static class ApplicationService
{
    public static ApplicationViewModel AppVM;
    public static IStorageProvider StorageProvider => ApplicationLifetime!.MainWindow!.StorageProvider;

    private static readonly IClassicDesktopStyleApplicationLifetime? ApplicationLifetime = Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    public static void Initialize()
    {
        AppVM = new ApplicationViewModel();
    }
    
    public static void Shutdown()
    {
        ApplicationLifetime?.Shutdown();
    }
}