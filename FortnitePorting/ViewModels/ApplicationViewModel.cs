using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Views;

namespace FortnitePorting.ViewModels;

public partial class ApplicationViewModel : ViewModelBase
{
    [ObservableProperty] private string versionString = $"v{Globals.VERSION}";
    
    [ObservableProperty] private UserControl? currentView;

    [ObservableProperty] private bool useFallbackBackground = AppSettings.Current.UseFallbackBackground || Environment.OSVersion.Platform != PlatformID.Win32NT || (Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version.Build < 22000);

    public ApplicationViewModel()
    {
        switch (AppSettings.Current.LoadingType)
        {
            case ELoadingType.Live:
            case ELoadingType.Local when AppSettings.Current.HasValidLocalData:
            case ELoadingType.Custom when AppSettings.Current.HasValidCustomData:
                SetView<MainView>();
                break;
            default:
                SetView<WelcomeView>();
                break;
        }
    }

    public async Task<string?> BrowseFolderDialog()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false });
        var folder = folders.ToArray().FirstOrDefault();

        return folder?.Path.AbsolutePath.Replace("%20", " ");
    }

    public async Task<string?> BrowseFileDialog(params FilePickerFileType[] fileTypes)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = fileTypes });
        var file = files.ToArray().FirstOrDefault();

        return file?.Path.AbsolutePath.Replace("%20", " ");
    }

    public void SetView<T>() where T : UserControl, new()
    {
        CurrentView = new T();
    }
}