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
    [ObservableProperty] private int _requestTimeoutSeconds = 10;
    [ObservableProperty] private bool _showMapDebugInfo = false;
    [ObservableProperty] private bool _isConsoleVisible = false;
}