using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    public override async Task OnViewExited()
    {
        if (AppSettings.ShouldSaveOnExit)
            AppSettings.Save();
    }

    [RelayCommand]
    public async void OpenDataFolder()
    {
        App.Launch(App.ApplicationDataFolder.FullName);
    }
    
    [RelayCommand]
    public async void Save()
    {
        AppSettings.Save();
        Info.Message("Settings", $"Successfully saved settings to {SettingsService.FilePath.FullName}");
    }

    [RelayCommand]
    public async void Reset()
    {
        App.RestartWithMessage("A restart is required", "To reset all settings, FortnitePorting must be restarted.", AppSettings.Reset);
    }
    
}