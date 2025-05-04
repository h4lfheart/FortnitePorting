using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using DesktopNotifications;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Views;
using FortnitePorting.Windows;

namespace FortnitePorting.Services;

public class AppService : IService
{
    public IClassicDesktopStyleApplicationLifetime Lifetime;
    public IStorageProvider StorageProvider => Lifetime.MainWindow!.StorageProvider;
    public IClipboard Clipboard => Lifetime.MainWindow!.Clipboard!;
    public INotificationManager? NotificationManager;
    
    public readonly DirectoryInfo DataFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".data"));
    public readonly DirectoryInfo AssetsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets"));
    public readonly DirectoryInfo PluginsFolder = new(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins"));
    
    public void InitializeDesktop(IClassicDesktopStyleApplicationLifetime desktop)
    {
        Lifetime = desktop;
        
        Initialize();
    }

    public void Initialize()
    {
        Info.CreateLogger();
        
        AppSettings.Load();
        Dependencies.Ensure();

        DataFolder.Create();
        AssetsFolder.Create();
        PluginsFolder.Create();

        Lifetime.Startup += OnAppStart;
        Lifetime.Exit += OnAppExit;
        Lifetime.MainWindow = new AppWindow();
    }

    private void OnAppStart(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        TimeWasterViewModel.LoadResources();

        TaskService.Run(AppWM.Initialize);
        
        if (AppSettings.Plugin.Blender.AutomaticallySync && Dependencies.FinishedEnsuring)
        {
            TaskService.Run(async () => await AppSettings.Plugin.Blender.SyncInstallations(verbose: false));
        }
        
        if (AppSettings.Installation.FinishedSetup)
        {
            Navigation.App.Open<HomeView>();
        }
        else
        {
            Navigation.App.Open<WelcomeView>();
        }
    }

    private void OnAppExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (AppSettings.ShouldSaveOnExit)
            AppSettings.Save();
    }
    
    public void Launch(string location, bool shellExecute = true)
    {
        Process.Start(new ProcessStartInfo { FileName = location, UseShellExecute = shellExecute });
    }
    
    public void LaunchSelected(string location)
    {
        var argument = "/select, \"" + location +"\"";
        Process.Start("explorer", argument);
    }
    
    public async Task<string?> BrowseFolderDialog(string startLocation = "")
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { AllowMultiple = false, SuggestedStartLocation = await StorageProvider.TryGetFolderFromPathAsync(startLocation)});
        var folder = folders.ToArray().FirstOrDefault();

        return folder?.Path.AbsolutePath.Replace("%20", " ");
    }

    public async Task<string?> BrowseFileDialog(string suggestedFileName = "", params FilePickerFileType[] fileTypes)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { AllowMultiple = false, FileTypeFilter = fileTypes, SuggestedFileName = suggestedFileName});
        var file = files.ToArray().FirstOrDefault();

        return file?.Path.AbsolutePath.Replace("%20", " ");
    }

    public async Task<string?> SaveFileDialog(string suggestedFileName = "", params FilePickerFileType[] fileTypes)
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions {FileTypeChoices = fileTypes, SuggestedFileName = suggestedFileName});
        return file?.Path.AbsolutePath.Replace("%20", " ");
    }

    public void RestartWithMessage(string title, string content, Action? onRestart = null, bool mandatory = false)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = "Restart",
            CloseButtonCommand = new RelayCommand(() =>
            {
                onRestart?.Invoke();
                Restart();
            }),
        };

        if (!mandatory)
        {
            dialog.PrimaryButtonText = "Cancel";
        }

        dialog.ShowAsync();
    }
    
    public void Restart()
    {
        Launch(AppDomain.CurrentDomain.FriendlyName, false);
        Shutdown();
    }

    public void Shutdown()
    {
        Lifetime.Shutdown();
    }
}