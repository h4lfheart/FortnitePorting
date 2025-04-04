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
using FortnitePorting.ViewModels.Plugin;
using FortnitePorting.ViewModels.Settings;
using NAudio.Wave;
using Newtonsoft.Json;

namespace FortnitePorting.ViewModels;

public partial class PluginViewModel : ViewModelBase
{
    [JsonIgnore] public Frame ContentFrame;
    [JsonIgnore] public NavigationView NavigationView;

    [ObservableProperty] private BlenderPluginViewModel _blender = new();
    [ObservableProperty] private UnrealPluginViewModel _unreal = new();
    

    public void Navigate(EExportLocation exportLocation)
    {
        var name = exportLocation.ToString();
        var viewName = $"FortnitePorting.Views.Plugin.{name}PluginView";
        
        var type = Type.GetType(viewName);
        if (type is null) return;
        
        ContentFrame.Navigate(type, null, AppSettings.Current.Application.Transition);

        NavigationView.SelectedItem = NavigationView.MenuItems
            .Concat(NavigationView.FooterMenuItems)
            .OfType<NavigationViewItem>()
            .FirstOrDefault(item => (EExportLocation) item.Tag! == exportLocation);
    }
}

