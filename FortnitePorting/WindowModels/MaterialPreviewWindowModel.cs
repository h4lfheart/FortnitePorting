using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.Material.Editor;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Leaderboard;
using FortnitePorting.Models.Nodes.Material;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Material;
using FortnitePorting.Rendering;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels;
using FortnitePorting.Views;
using FortnitePorting.Windows;
using Microsoft.VisualBasic.Logging;
using ScottPlot.Colormaps;
using ColorSpectrumShape = Avalonia.Controls.ColorSpectrumShape;
using Log = Serilog.Log;
using Orientation = Avalonia.Layout.Orientation;

namespace FortnitePorting.WindowModels;

[Transient]
public partial class MaterialPreviewWindowModel(SettingsService settings) : WindowModelBase
{
    [ObservableProperty] private SettingsService _settings = settings;
    
    [ObservableProperty] private ObservableCollection<MaterialNodeTree> _trees = [];
    [ObservableProperty] private MaterialNodeTree? _selectedTree;
    
    [ObservableProperty] private uint _gridSpacing = 25;
    
    
    [ObservableProperty] private Brush _backgroundBrush = new LinearGradientBrush
    {
        StartPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative),
        GradientStops = 
        [
            new GradientStop(Color.Parse("#9D111111"), 0),
            new GradientStop(Color.Parse("#82212121"), 1),
        ]
    };
    
    [RelayCommand]
    public async Task Preview(FPackageIndex index)
    {
        var asset = await index.LoadOrDefaultAsync<UObject>();
        if (asset is null) return;

        await FilesVM.PreviewAsset(asset);
    }
    
    [RelayCommand]
    public async Task NavigateToPath(FSoftObjectPath path)
    {
        var asset = await path.LoadOrDefaultAsync<UObject>();
        if (asset is null) return;

        FilesVM.FileViewJumpTo(UEParse.Provider.FixPath(asset.GetPathName().SubstringBefore(".")));
        Navigation.App.Open<FilesView>();
        AppWM.Window.BringToTop();
    }

    [RelayCommand]
    public async Task NavigateTo(FPackageIndex index)
    {
        var asset = await index.LoadOrDefaultAsync<UObject>();
        if (asset is null) return;

        FilesVM.FileViewJumpTo(UEParse.Provider.FixPath(asset.GetPathName().SubstringBefore(".")));
        Navigation.App.Open<FilesView>();
        AppWM.Window.BringToTop();
    }

    public void Load(UObject obj)
    {
        if (Trees.FirstOrDefault(data => data.Asset?.Equals(obj) ?? false) is { } existingData)
        {
            SelectedTree = existingData;
        }
        else
        {
            var data = new MaterialNodeTree();
            data.Load(obj);
            Trees.Add(data);
            SelectedTree = data;
        }
    }
    
    public void Load(MaterialNodeTree nodeTree)
    {
        if (Trees.FirstOrDefault(tree => tree.TreeName.Equals(nodeTree.TreeName)) is { } existingTree)
        {
            SelectedTree = existingTree;
        }
        else
        {
            Trees.Add(nodeTree);
            SelectedTree = nodeTree;
        }
    }
}

