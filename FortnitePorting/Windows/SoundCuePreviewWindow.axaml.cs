using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using CUE4Parse.UE4.Assets.Exports;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.SoundCue;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class SoundCuePreviewWindow : WindowBase<SoundCuePreviewWindowModel>
{
    public static SoundCuePreviewWindow? Instance;
    
    public SoundCuePreviewWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    public static void Preview(UObject obj)
    {
        if (Instance is null)
        {
            Instance = new SoundCuePreviewWindow();
            Instance.Show();
            Instance.BringToTop();
        }

        if (Instance.WindowModel.LoadedSoundCueDatas.FirstOrDefault(mat => mat.Asset?.Name.Equals(obj.Name) ?? false) is
            { } existing)
        {
            Instance.WindowModel.SelectedSoundCueData = existing;
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
        if (e.ClickCount != 2) return;
        if (sender is not Control control) return;
        if (control.DataContext is not SoundCueNode node) return;
        
        if (node.LinkedNode is not null)
        {
            Editor.ViewportZoom = 1;
            Editor.ViewportLocation = new Point(node.LinkedNode.Location.X - Editor.ViewportSize.Width / 2, node.LinkedNode.Location.Y - Editor.ViewportSize.Height / 2);
            Editor.SelectedItem = node.LinkedNode;
        }
    }

    private void OnTabClosed(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is not SoundCueData data) return;

        WindowModel.LoadedSoundCueDatas.Remove(data);

        if (WindowModel.LoadedSoundCueDatas.Count == 0)
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
        var nodes = WindowModel.SelectedSoundCueData?.NodeCache.Items.ToArray() ?? [];
        var avgX = nodes.Sum(node => node.Location.X) / nodes.Length;
        var avgY = nodes.Sum(node => node.Location.Y) / nodes.Length;

        Editor.ViewportLocation = new Point(avgX - Editor.ViewportSize.Width / 2, avgY - Editor.ViewportSize.Height / 2);
    }

    private void OnSearchSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not SoundCueNodeBase selectedNode) return;

        WindowModel.SelectedSoundCueData.SelectedNode = selectedNode;
        Editor.ViewportLocation = new Point(selectedNode.Location.X - Editor.ViewportSize.Width / 2, selectedNode.Location.Y - Editor.ViewportSize.Height / 2);
    }
}