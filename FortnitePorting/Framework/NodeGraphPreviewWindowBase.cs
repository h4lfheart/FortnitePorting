using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.Nodes;
using FortnitePorting.WindowModels;
using Nodify;
using Node = FortnitePorting.Models.Nodes.Node;

namespace FortnitePorting.Framework;

public abstract class NodeGraphPreviewWindowBase<TWindow, TModel, TTree> : PreviewWindowBase<TWindow, TModel>
    where TWindow : NodeGraphPreviewWindowBase<TWindow, TModel, TTree>
    where TModel : NodeGraphPreviewWindowModelBase<TTree>
    where TTree : NodeTree, new()
{
    protected bool IsNodePress;
    protected NodifyEditor? GraphEditor;

    protected NodeGraphPreviewWindowBase()
    {
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        GraphEditor ??= this.FindControl<NodifyEditor>("Editor");
    }

    protected void OnTabClosed(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        RemoveTabAndCloseIfEmpty(WindowModel.Trees, args.Item);
    }

    protected void OnTabSelectionChanged(object? sender, SelectionChangedEventArgs args)
    {
        CenterViewport();
    }

    protected void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Home)
        {
            CenterViewport();
        }
    }

    protected void CenterViewport()
    {
        if (GraphEditor is null) return;

        var nodes = WindowModel.SelectedTree?.NodeCache.Items.ToArray() ?? [];
        if (nodes.Length == 0) return;

        var avgX = nodes.Sum(node => node.Location.X) / nodes.Length;
        var avgY = nodes.Sum(node => node.Location.Y) / nodes.Length;

        GraphEditor.ViewportLocation = new Point(avgX - GraphEditor.ViewportSize.Width / 2, avgY - GraphEditor.ViewportSize.Height / 2);
    }

    protected void OnSearchSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (GraphEditor is null) return;
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not BaseNode selectedNode) return;
        if (WindowModel.SelectedTree is null) return;

        WindowModel.SelectedTree.SelectedNode = selectedNode;
        if (!IsNodePress)
        {
            GraphEditor.ViewportLocation = new Point(
                selectedNode.Location.X - GraphEditor.ViewportSize.Width / 2,
                selectedNode.Location.Y - GraphEditor.ViewportSize.Height / 2);
        }

        IsNodePress = false;
    }

    protected void FocusLinkedNode(Node node)
    {
        if (GraphEditor is null || node.LinkedNode is null) return;

        GraphEditor.ViewportZoom = 1;
        GraphEditor.ViewportLocation = new Point(
            node.LinkedNode.Location.X - GraphEditor.ViewportSize.Width / 2,
            node.LinkedNode.Location.Y - GraphEditor.ViewportSize.Height / 2);
        GraphEditor.SelectedItem = node.LinkedNode;
    }
}
