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
using CUE4Parse_Conversion.Materials;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.Math;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Nodes;
using FortnitePorting.Models.Nodes.Material;
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
    
    private bool isNodePress = false;
    
    public MaterialPreviewWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    public static void Preview(UObject obj)
    {
        if (Instance is null)
        {
            Instance = new MaterialPreviewWindow();
            Instance.Show();
        }
        
        Instance.BringToTop();

        if (Instance.WindowModel.Trees.FirstOrDefault(mat => mat.Asset?.Name.Equals(obj.Name) ?? false) is
            { } existing)
        {
            Instance.WindowModel.SelectedTree = existing;
            return;
        }
        
        Instance.WindowModel.Load(obj);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance = null;
    }

    private void OnNodePressed(object? sender, PointerPressedEventArgs e)
    {
        isNodePress = true;
        
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
            WindowModel.Load(node.Subgraph as MaterialNodeTree);
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
        if (args.Item is not MaterialNodeTree tree) return;

        WindowModel.Trees.Remove(tree);

        if (WindowModel.Trees.Count == 0)
        {
            Close();
        }
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
        var nodes = WindowModel.SelectedTree?.NodeCache.Items.ToArray() ?? [];
        var avgX = nodes.Sum(node => node.Location.X) / nodes.Length;
        var avgY = nodes.Sum(node => node.Location.Y) / nodes.Length;

        Editor.ViewportLocation = new Point(avgX - Editor.ViewportSize.Width / 2, avgY - Editor.ViewportSize.Height / 2);
    }

    
    private void OnSearchSelectionChanged(object? sender, SelectionChangedEventArgs selectionChangedEventArgs)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not BaseNode selectedNode) return;

        WindowModel.SelectedTree.SelectedNode = selectedNode;
        if (!isNodePress)
            Editor.ViewportLocation = new Point(selectedNode.Location.X - Editor.ViewportSize.Width / 2, selectedNode.Location.Y - Editor.ViewportSize.Height / 2);

        isNodePress = false;
    }
}