using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Material;
using FortnitePorting.Models.Unreal.Material;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
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
        Owner = App.Lifetime.MainWindow;
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
    
    private void OnNodePressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if (sender is not Control control) return;
        if (control.DataContext is not MaterialNode node) return;

        if (node.Package is not null && !node.Package.IsNull)
        {
            var package = node.Package.Load();
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

        if (node.Subgraph is not null)
        {
            WindowModel.Load(node.Subgraph);
        }

        if (node.LinkedNode is not null)
        {
            Editor.ViewportZoom = 1;
            Editor.ViewportLocation = new Point(node.LinkedNode.Location.X - Editor.ViewportSize.Width / 2, node.LinkedNode.Location.Y - Editor.ViewportSize.Height / 2);
            Editor.SelectedItem = node.LinkedNode;
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

    private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs args)
    {
        CenterViewport();
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Home)
        {
            CenterViewport();
        }
    }

    private void CenterViewport()
    {
        var nodes = WindowModel.SelectedMaterialData?.NodeCache.Items.ToArray() ?? [];
        var avgX = nodes.Sum(node => node.Location.X) / nodes.Length;
        var avgY = nodes.Sum(node => node.Location.Y) / nodes.Length;

        Editor.ViewportLocation = new Point(avgX - Editor.ViewportSize.Width / 2, avgY - Editor.ViewportSize.Height / 2);
    }

    private void OnSearchSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not MaterialNodeBase selectedNode) return;

        WindowModel.SelectedMaterialData.SelectedNode = selectedNode;
        Editor.ViewportLocation = new Point(selectedNode.Location.X - Editor.ViewportSize.Width / 2, selectedNode.Location.Y - Editor.ViewportSize.Height / 2);
    }
}