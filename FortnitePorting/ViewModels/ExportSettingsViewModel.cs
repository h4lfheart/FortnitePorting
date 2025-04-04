using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Animations;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.UEFormat.Enums;
using CUE4Parse.UE4.Versions;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.Models.Radio;
using FortnitePorting.Shared;
using FortnitePorting.ViewModels.Settings;
using NAudio.Wave;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels;

public partial class ExportSettingsViewModel : ViewModelBase
{
    [JsonIgnore] public Frame ContentFrame;
    [JsonIgnore] public NavigationView NavigationView;
    
    [ObservableProperty] private BlenderSettingsViewModel _blender = new();
    [ObservableProperty] private UnrealSettingsViewModel _unreal = new();
    [ObservableProperty] private FolderSettingsViewModel _folder = new();

    public override async Task OnViewExited()
    {
        AppSettings.Save();
    }

    public void Navigate(EExportLocation exportLocation)
    {
        var name = exportLocation is EExportLocation.AssetsFolder or EExportLocation.CustomFolder ? "Folder" : exportLocation.ToString();
        var viewName = $"FortnitePorting.Views.Settings.{name}SettingsView";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        ContentFrame.Navigate(type, null, AppSettings.Current.Application.Transition);

        NavigationView.SelectedItem = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => (EExportLocation) item.Tag! == exportLocation);
    }
}

public partial class BaseExportSettings : ViewModelBase
{
    [ObservableProperty] private EFileCompressionFormat _compressionFormat = EFileCompressionFormat.ZSTD;

    [ObservableProperty] private EImageFormat _imageFormat = EImageFormat.PNG;
    [ObservableProperty] private bool _exportMaterials = true;
    
    [ObservableProperty] private EMeshFormat _meshFormat = EMeshFormat.UEFormat;
    [ObservableProperty] private bool _importInstancedFoliage = true;
    
    [ObservableProperty] private EAnimFormat _animFormat = EAnimFormat.UEFormat;
    [ObservableProperty] private bool _importLobbyPoses = false;
    
    [ObservableProperty] private ESoundFormat _soundFormat = ESoundFormat.WAV;
    
    public virtual ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions()
        {
            MeshFormat = MeshFormat,
            AnimFormat = AnimFormat,
            CompressionFormat = CompressionFormat
        };
    }
}

