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
using FortnitePorting.Extensions;
using FortnitePorting.Models.Material;
using FortnitePorting.Models.Unreal;
using FortnitePorting.Models.Unreal.Material;
using FortnitePorting.Rendering;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.Windows;
using Microsoft.VisualBasic.Logging;
using ScottPlot.Colormaps;
using ColorSpectrumShape = Avalonia.Controls.ColorSpectrumShape;
using Log = Serilog.Log;
using Orientation = Avalonia.Layout.Orientation;

namespace FortnitePorting.WindowModels;

public partial class MaterialPreviewWindowModel : WindowModelBase
{
    [ObservableProperty] private ObservableCollection<MaterialData> _loadedMaterialDatas = [];
    [ObservableProperty] private MaterialData? _selectedMaterialData;
    
    
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

    public void Load(UObject obj)
    {
        if (LoadedMaterialDatas.FirstOrDefault(data => data.Asset?.Equals(obj) ?? false) is { } existingData)
        {
            SelectedMaterialData = existingData;
        }
        else
        {
            var data = new MaterialData();
            data.Load(obj);
            LoadedMaterialDatas.Add(data);
            SelectedMaterialData = data;
        }
    }
    
    public void Load(MaterialData materialData)
    {
        if (LoadedMaterialDatas.FirstOrDefault(data => data.DataName.Equals(materialData.DataName)) is { } existingData)
        {
            SelectedMaterialData = existingData;
        }
        else
        {
            LoadedMaterialDatas.Add(materialData);
            SelectedMaterialData = materialData;
        }
    }
}

