using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AsyncImageLoader;
using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.Application;
using FortnitePorting.Shared.Models;
using Newtonsoft.Json;
using Serilog;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace FortnitePorting.Launcher.Models.Installation;

public partial class InstallationProfile : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _name;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(DescriptionString))] private FPVersion _version;
    [ObservableProperty] private string _directory;
    [ObservableProperty] private string _executableName;
    [ObservableProperty] private EProfileType _profileType;
    
    [ObservableProperty] private string? _iconUrl;
    [ObservableProperty] private string? _repositoryUrl;

    [JsonIgnore] public string ExecutablePath => Path.Combine(Directory, ExecutableName);
    [JsonIgnore] public string DescriptionString => $"{Version} - {Id}";
    [JsonIgnore] public Task<Bitmap?> IconImage => ImageLoader.AsyncImageLoader.ProvideImageAsync(IconUrl ?? string.Empty);

    public async Task Launch()
    {
        AppWM.Message("Launch", $"Launching {Name}");
        
        Process.Start(new ProcessStartInfo
        {
            FileName = ExecutablePath,
            UseShellExecute = true
        });
    }
    
    public async Task Rename()
    {
        var textBox = new TextBox
        {
            Watermark = "New Profile Name"
        };
        
        var dialog = new ContentDialog
        {
            Title = $"Rename \"{Name}\"",
            Content = textBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Rename",
            PrimaryButtonCommand = new RelayCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(textBox.Text)) return;
                
                Name = textBox.Text;
            })
        };

        await dialog.ShowAsync();
    }
    
    public async Task OpenFolder()
    {
        LaunchSelected(ExecutablePath);
    }
    
    public async Task ChangeVersionPrompt()
    {
        var comboBox = new ComboBox
        {
            ItemsSource = AppSettings.Current.DownloadedVersions,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        var dialog = new ContentDialog
        {
            Title = $"Change Version of \"{Name}\"",
            Content = comboBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Change",
            PrimaryButtonCommand = new RelayCommand(() =>
            {
                if (comboBox.SelectedItem is not InstallationVersion newVersion) return;

                ChangeVersion(newVersion);
            })
        };

        await dialog.ShowAsync();
    }
    
    public async Task Update()
    {
        await Update(verbose: true);
    }

    public async Task Update(bool verbose)
    {
        if (ProfileType != EProfileType.Repository) return;
        
        var targetRepository = RepositoriesVM.Repositories.FirstOrDefault(repo => repo.RepositoryUrl.Equals(RepositoryUrl));
            
        var newestVersion = targetRepository?.Versions.MaxBy(version => version.Version);
        if (newestVersion is null) return;

        if (newestVersion.Version <= Version)
        {
            if (verbose)
                AppWM.Message("Update", $"{Name} is up to date");
            return;
        }

        var oldVersion = Version;
        ChangeVersion(await newestVersion.DownloadInstallationVersion(), verbose: false);
            
        if (verbose)
            AppWM.Message("Update", $"{Name} was updated from \"{oldVersion}\" to \"{Version}\"");
        
        Log.Information($"{Name} was updated from \"{oldVersion}\" to \"{Version}\"");
    }

    public void ChangeVersion(InstallationVersion newVersion, bool verbose = true)
    {
        File.Delete(ExecutablePath);
        File.Copy(newVersion.ExecutablePath, ExecutablePath);
        ExecutableName = Path.GetFileName(newVersion.ExecutablePath);
        
        Version = newVersion.Version;
        
        if (verbose)
            AppWM.Message("Update", $"{Name} was changed to \"{Version}\"");
    }

    public async Task DeleteAndCleanup()
    {
        System.IO.Directory.Delete(Directory, true);
    }
    
}

public enum EProfileType
{
    [Description("From Repository")]
    Repository,
    
    [Description("Custom Version")]
    Custom
}