using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Framework;

namespace FortnitePorting.ViewModels.Settings;

public partial class DeveloperSettingsViewModel : SettingsViewModelBase
{
     [ObservableProperty, NotifyPropertyChangedFor(nameof(PortlePath))] private string _portleExecutablePath;
     [ObservableProperty, NotifyPropertyChangedFor(nameof(PortlePath))] private bool _usePortlePath;
     
     public string PortlePath => UsePortlePath && File.Exists(PortleExecutablePath) 
          ? PortleExecutablePath 
          : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Portle", "Portle.exe");

     
     [ObservableProperty] private int _chunkCacheLifetime = 1;
     [ObservableProperty] private int _requestTimeoutSeconds = 60;
     
     public async Task BrowsePortlePath()
     {
          if (await App.BrowseFileDialog() is { } path) PortleExecutablePath = path;
     }

}