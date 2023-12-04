using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Services;

public static class ApplicationService
{
    public static IClassicDesktopStyleApplicationLifetime Application;
    public static Window MainWindow => Application.MainWindow!;
    public static IStorageProvider StorageProvider => MainWindow.StorageProvider;
    public static IClipboard Clipboard => MainWindow.Clipboard;

    public static ApplicationViewModel AppVM = null!;
    public static MainViewModel MainVM => ViewModelRegistry.Get<MainViewModel>()!;
    public static HomeViewModel HomeVM => ViewModelRegistry.Get<HomeViewModel>()!;
    public static CUE4ParseViewModel CUE4ParseVM => ViewModelRegistry.Get<CUE4ParseViewModel>()!;
    public static AssetsViewModel AssetsVM => ViewModelRegistry.Get<AssetsViewModel>()!;
    public static FilesViewModel FilesVM => ViewModelRegistry.Get<FilesViewModel>()!;
    public static RadioViewModel RadioVM => ViewModelRegistry.Get<RadioViewModel>()!;
}