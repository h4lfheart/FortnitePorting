using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Launcher.Application;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace FortnitePorting.Launcher.Models.Installation;

public partial class InstallationProfile : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _versionName;
    [ObservableProperty] private string _directory;
    [ObservableProperty] private string _executableName;
    [ObservableProperty] private EProfileType _profileType;

    public string ExecutablePath => Path.Combine(Directory, ExecutableName);

    public async Task Launch()
    {
        AppWM.Message(Name, $"Launching {VersionName}");
        
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
    
    public async Task ChangeVersion()
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

                if (Name.Equals(VersionName)) // if version name is identical to display name, change it to new version name
                {
                    Name = newVersion.Name;
                }
                
                File.Delete(ExecutablePath);
                File.Copy(newVersion.ExecutablePath, ExecutablePath);
                ExecutableName = Path.GetFileName(newVersion.ExecutablePath);
            })
        };

        await dialog.ShowAsync();
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