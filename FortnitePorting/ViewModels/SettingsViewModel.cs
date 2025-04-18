using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.UE4.Versions;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Export.Models;
using FortnitePorting.Framework;
using FortnitePorting.Models.API;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Radio;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.ViewModels.Settings;
using NAudio.Wave;
using Newtonsoft.Json;
using RestSharp;

namespace FortnitePorting.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    [JsonIgnore] public Frame ContentFrame;
    [JsonIgnore] public NavigationView NavigationView;
    
    // ViewModels
    [ObservableProperty] private ExportSettingsViewModel _exportSettings = new();
    [ObservableProperty] private InstallationSettingsViewModel _installation = new();
    [ObservableProperty] private ApplicationSettingsViewModel _application = new();
    [ObservableProperty] private ThemeSettingsViewModel _theme = new();
    [ObservableProperty] private OnlineSettingsViewModel _online = new();
    [ObservableProperty] private PluginViewModel _plugin = new();
    [ObservableProperty] private DebugSettingsViewModel _debug = new();
    
    // Assets
    [ObservableProperty] private HashSet<string> _favoriteAssets = [];

    // Radio
    [ObservableProperty] private RadioPlaylistSerializeData[] _playlists = [];
    [ObservableProperty] private int _audioDeviceIndex = 0;
    [ObservableProperty] private float _volume = 1.0f;

    public override async Task OnViewExited()
    {
        AppSettings.Save();
    }


    public ExportDataMeta CreateExportMeta(EExportLocation exportLocation = EExportLocation.Blender, string? customPath = null) => new()
    {
        ExportLocation = exportLocation,
        AssetsRoot = Application.AssetPath,
        Settings = exportLocation switch
        {
            EExportLocation.Blender => ExportSettings.Blender,
            EExportLocation.Unreal => ExportSettings.Unreal,
            EExportLocation.AssetsFolder => ExportSettings.Folder,
            EExportLocation.CustomFolder => ExportSettings.Folder
        },
        CustomPath = customPath
    };
    
    public void Navigate<T>()
    {
        Navigate(typeof(T));
    }
    
    public void Navigate(Type type)
    {
        ContentFrame.Navigate(type, null, AppSettings.Current.Application.Transition);

        var buttonName = type.Name.Replace("SettingsView", string.Empty);
        NavigationView.SelectedItem = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => (string) item.Tag! == buttonName);
    }
    
}