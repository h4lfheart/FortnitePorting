using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Media.Animation;
using FortnitePorting.Framework;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Validators;
using NAudio.Wave;
using Newtonsoft.Json;
using Serilog;

namespace FortnitePorting.ViewModels.Settings;

public partial class DebugSettingsViewModel : ViewModelBase
{
    [ObservableProperty] private int _chunkCacheLifetime = 1;
    [ObservableProperty] private bool _downloadDebuggingSymbols;
    [ObservableProperty] private int _requestTimeoutSeconds = 10;
    [ObservableProperty] private bool _showMapDebugInfo = false;
    [ObservableProperty] private bool _isConsoleVisible = false;
    
    protected override async void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(DownloadDebuggingSymbols):
            {
                if (ApiVM is null) break;
                
                var executingDirectory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                if (DownloadDebuggingSymbols)
                {
                    var fileNames = await ApiVM.FortnitePorting.GetReleaseFilesAsync();
                    var pdbFiles = fileNames.Where(fileName => fileName.EndsWith(".pdb"));
                    foreach (var pdbFile in pdbFiles) await ApiVM.DownloadFileAsync(pdbFile, executingDirectory);
                }
                else
                {
                    var pdbFiles = executingDirectory.GetFiles("*.pdb");
                    foreach (var pdbFile in pdbFiles) pdbFile.Delete();
                }
                
                AppWM.Message("Debugging Symbols", "Finished downloading debugging symbols. Please restart the application.");

                break;
            }
            case nameof(ShowMapDebugInfo):
            {
                if (MapVM is not null)
                    MapVM.ShowDebugInfo = ShowMapDebugInfo;
                break;
            }
            case nameof(IsConsoleVisible):
            {
                AppWM.ConsoleIsVisible = IsConsoleVisible;
                break;
            }
        }
    }
}