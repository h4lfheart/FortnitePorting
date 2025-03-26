using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.Material;
using FortnitePorting.Models.Unreal.Material;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.WindowModels;
using Nodify;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Serilog;

namespace FortnitePorting.Windows;

public partial class MaterialPreviewWindow : WindowBase<MaterialPreviewWindowModel>
{
    public static MaterialPreviewWindow? Instance;
    
    public MaterialPreviewWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = ApplicationService.Application.MainWindow;
    }

    public static void Preview(UObject obj)
    {
        if (Instance is not null)
        {
            Instance.WindowModel.Load(obj);
            Instance.BringToTop();
            return;
        }
        
        TaskService.RunDispatcher(() =>
        {
            Instance = new MaterialPreviewWindow();
            Instance.WindowModel.Load(obj);
            Instance.Show();
            Instance.BringToTop();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance = null;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not NodifyEditor editor) return;
        if (editor.SelectedItem is not MaterialNode node) return;

        WindowModel.SelectedMaterialData.SelectedNode = node;
    }

    private void OnNodePressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if (sender is not Control control) return;
        if (control.DataContext is not MaterialNode node) return;
        if (node.DoubleClickPackage is null || node.DoubleClickPackage.IsNull) return;

        var package = node.DoubleClickPackage.Load();
        switch (package)
        {
            case UMaterial material:
            {
                Preview(material);
                break;
            }
            case UMaterialFunction materialFunction:
            {
                Preview(materialFunction);
                break;
            }
        }
    }

    private void OnTabClosed(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is not MaterialData data) return;

        WindowModel.LoadedMaterialDatas.Remove(data);

        if (WindowModel.LoadedMaterialDatas.Count == 0)
        {
            Close();
        }
    }

    private void OnTitleBarPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}