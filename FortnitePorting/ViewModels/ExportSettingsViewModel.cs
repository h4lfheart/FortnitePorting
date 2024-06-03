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
using FortnitePorting.Models.Radio;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.ExportSettings;
using FortnitePorting.Views.ExportSettings;
using NAudio.Wave;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels;

public partial class ExportSettingsViewModel : ViewModelBase
{
    [JsonIgnore] public Frame ContentFrame;
    [JsonIgnore] public NavigationView NavigationView;
    
    [ObservableProperty] private BlenderExportSettingsViewModel _blender = new();
    [ObservableProperty] private UnrealExportSettingsViewModel _unreal = new();
    [ObservableProperty] private FolderExportSettingsViewModel _folder = new();

    public void Navigate(EExportLocation exportLocation)
    {
        var name = exportLocation is EExportLocation.AssetsFolder or EExportLocation.CustomFolder ? "Folder" : exportLocation.ToString();
        var viewName = $"FortnitePorting.Views.ExportSettings.{name}ExportSettingsView";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        ContentFrame.Navigate(type, null, Globals.TransitionInfo);

        NavigationView.SelectedItem = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => (EExportLocation) item.Tag! == exportLocation);
    }
}

public partial class BaseExportSettings : ViewModelBase
{
    [ObservableProperty] private EMeshFormat _meshFormat = EMeshFormat.UEFormat;
    [ObservableProperty] private EAnimFormat _animFormat = EAnimFormat.UEFormat;
    [ObservableProperty] private EFileCompressionFormat _compressionFormat = EFileCompressionFormat.ZSTD;
    [ObservableProperty] private EImageFormat _imageType = EImageFormat.PNG;

    [ObservableProperty] private bool _exportMaterials = true;
    
    public virtual ExporterOptions CreateExportOptions()
    {
        return new ExporterOptions();
    }
}

