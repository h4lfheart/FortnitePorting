using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels;

public partial class SettingsViewModel(SettingsService settings, SupabaseService supabase) : ViewModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;
    [ObservableProperty] private SupabaseService _supaBase = supabase;
    
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
    public void ClearCache()
    {
        const string messageId = "ClearCache";
        Info.Message("Clear Cache", "Clearing cached files...", id: messageId, autoClose: false);

        TaskService.Run(() =>
        {
            var (cleared, skipped) = ClearFolder(UEParse.CacheFolder);
            var dataResult = ClearFolder(App.DataFolder);
            cleared += dataResult.Cleared;
            skipped += dataResult.Skipped;

            Info.CloseMessage(messageId);

            var message = skipped > 0
                ? $"Cleared {cleared} files ({skipped} skipped due to access errors)."
                : $"Successfully cleared {cleared} files.";
            Info.Message("Clear Cache", message);
        });
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

    [RelayCommand]
    public void RestartApplication()
    {
        App.RestartWithMessage("Restart Application", "Are you sure you would like to restart?");
    }

    private static (int Cleared, int Skipped) ClearFolder(DirectoryInfo folder)
    {
        if (!folder.Exists) 
            return (0, 0);

        var cleared = 0;
        var skipped = 0;

        foreach (var file in folder.EnumerateFiles("*", SearchOption.AllDirectories))
        {
            try
            {
                file.IsReadOnly = false;
                file.Delete();
                cleared++;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                skipped++;
            }
        }

        foreach (var directory in folder.EnumerateDirectories("*", SearchOption.AllDirectories)
                     .OrderByDescending(directory => directory.FullName.Length))
        {
            try
            {
                if (!directory.EnumerateFileSystemInfos().Any())
                    directory.Delete();
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
            }
        }

        folder.Create();
        return (cleared, skipped);
    }
}
