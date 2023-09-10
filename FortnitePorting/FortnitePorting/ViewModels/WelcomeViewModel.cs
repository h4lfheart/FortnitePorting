using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using CUE4Parse.Utils;
using FortnitePorting.Framework;
using FortnitePorting.Views;

namespace FortnitePorting.ViewModels;

public partial class WelcomeViewModel : ViewModelBase
{
    public bool CanContinue => CurrentLoadingType switch
    {
        ELoadingType.Local => !string.IsNullOrWhiteSpace(LocalArchivePath) && Directory.Exists(LocalArchivePath),
        ELoadingType.Live => true,
        ELoadingType.Custom => !(string.IsNullOrWhiteSpace(LocalArchivePath) || string.IsNullOrWhiteSpace(CustomMappingsPath) || string.IsNullOrWhiteSpace(CustomEncryptionKey)) && Directory.Exists(CustomArchivePath) && File.Exists(CustomMappingsPath) && CustomEncryptionKey.TryParseAesKey(out _),
    };
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanContinue))] private ELoadingType currentLoadingType;  
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanContinue))] private string localArchivePath = "???";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanContinue))] private string customArchivePath = "???";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanContinue))] private string customMappingsPath = "???";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(CanContinue))] private string customEncryptionKey = Globals.ZERO_CHAR;
    [ObservableProperty] private EGame customUnrealVersion = EGame.GAME_UE5_3;

    private static readonly FilePickerFileType MappingsFileType = new("Unreal Mappings")
    {
        Patterns = new[] { "*.usmap" }
    };
    
    [RelayCommand]
    private async Task BrowseLocalArchivePath()
    {
        if (await AppVM.BrowseFolderDialog() is {} path)
        {
            LocalArchivePath = path;
        }
    }
    
    [RelayCommand]
    private async Task BrowseCustomArchivePath()
    {
        if (await AppVM.BrowseFolderDialog() is {} path)
        {
            CustomArchivePath = path;
        }
    }

    [RelayCommand]
    private async Task BrowseMappingsFile()
    {
        if (await AppVM.BrowseFileDialog(MappingsFileType) is {} path)
        {
            CustomMappingsPath = path;
        }
    }
    
    [RelayCommand]
    private void Continue()
    {
        AppVM.SetView<LoadingView>();
    }
}