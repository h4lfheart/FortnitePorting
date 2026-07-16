using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Controls;
using FortnitePorting.Controls.WrapPanel;
using FortnitePorting.Models.Files;
using FortnitePorting.Services;
using Lucdem.Avalonia.SourceGenerators.Attributes;
using Newtonsoft.Json;

namespace FortnitePorting.Controls.Files;

public partial class FileBrowser : UserControl
{
    [AvaDirectProperty] private FileBrowserContext _context;

    public event Action<TreeItem>? FileItemDoubleTapped;

    private bool _suppressSelectionChange;
    private bool _syncingVfsFilterSelection;
    private PointerPressedEventArgs? _dragPressArgs;
    private Point _dragStartPosition;

    public FileBrowser()
    {
        InitializeComponent();

        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!Context.UseFlatView)
        {
            if (TryHandleNavigationButton(e))
                return;
        }

        if (!Context.IsDragDropEnabled) return;
        if (e.Source is not Control source) return;

        if (IsSourceAlreadySelected(source))
        {
            e.Handled = true;
            _suppressSelectionChange = true;
        }

        _dragPressArgs = e;
        _dragStartPosition = e.GetPosition(this);
    }

    private bool TryHandleNavigationButton(PointerPressedEventArgs e)
    {
        var point = e.GetCurrentPoint(this);

        if (point.Properties.IsXButton1Pressed && Context.CanGoBack)
        {
            Context.GoBack();
            e.Handled = true;
            return true;
        }

        if (point.Properties.IsXButton2Pressed && Context.CanGoForward)
        {
            Context.GoForward();
            e.Handled = true;
            return true;
        }

        return false;
    }

    private bool IsSourceAlreadySelected(Control source)
    {
        if (Context.UseFlatView)
            return source.DataContext is FlatItem fi && Context.IsFlatItemSelected(fi);

        return source.DataContext is TreeItem ti && Context.SelectedFileViewItems.Contains(ti);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_dragPressArgs is null || !Context.IsDragDropEnabled) return;

        var pos = e.GetPosition(this);
        var delta = pos - _dragStartPosition;
        if (Math.Abs(delta.X) < 8 && Math.Abs(delta.Y) < 8) return;

        var args = _dragPressArgs;
        ResetDragState();

        TaskService.RunDispatcher(async () => await StartDragAsync(args));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var suppress = _suppressSelectionChange;
        var pressArgs = _dragPressArgs;
        ResetDragState();

        if (!suppress || pressArgs is null) return;
        if (e.Source is not Control source) return;

        ApplySelectionFromRelease(source, e.KeyModifiers);
    }

    private void ApplySelectionFromRelease(Control source, KeyModifiers modifiers)
    {
        var isMultiSelect = (modifiers & KeyModifiers.Control) != 0
                         || (modifiers & KeyModifiers.Shift) != 0;

        if (Context.UseFlatView)
        {
            if (source.DataContext is not FlatItem flatItem) return;
            UpdateSelection(Context.SelectedFlatViewItems, flatItem, isMultiSelect);
        }
        else
        {
            if (source.DataContext is not TreeItem treeItem) return;
            UpdateSelection(Context.SelectedFileViewItems, treeItem, isMultiSelect);
        }
    }

    private static void UpdateSelection<T>(IList<T> selectedItems, T item, bool isMultiSelect)
    {
        if (isMultiSelect)
        {
            if (!selectedItems.Remove(item))
                selectedItems.Add(item);
        }
        else
        {
            selectedItems.Clear();
            selectedItems.Add(item);
        }
    }

    private void ResetDragState()
    {
        _suppressSelectionChange = false;
        _dragPressArgs = null;
    }

    private async Task StartDragAsync(PointerEventArgs e)
    {
        var paths = Context.GetSelectedFilePaths();
        if (paths.Length == 0) return;

        var dragDropInfoFile = await WriteDragDropInfoAsync(paths);

        TaskService.Run(ExportClient.DiscoverAsync);

        var storageFile = await TopLevel.GetTopLevel(this)!
            .StorageProvider
            .TryGetFileFromPathAsync(new Uri(dragDropInfoFile));

        if (storageFile is null) return;

        var data = new DataObject();
        data.Set(DataFormats.Files, new[] { storageFile });

        await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy | DragDropEffects.Move);
    }

    private static async Task<string> WriteDragDropInfoAsync(string[] paths)
    {
        var filePath = Path.Combine(App.DataFolder.FullName, "info.fp_drag_drop");
        var payload = JsonConvert.SerializeObject(new { Paths = paths.ToList() });
        await File.WriteAllTextAsync(filePath, payload);
        return filePath;
    }

    private void OnTreeItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView treeView) return;
        if (treeView.SelectedItem is not TreeItem { Type: ENodeType.Folder } item) return;

        Context.LoadFileItems(item);
    }

    private void OnTreeItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not TreeView treeView) return;
        if (treeView.SelectedItem is not TreeItem item) return;

        if (item.Type == ENodeType.Folder)
        {
            item.Expanded = !item.Expanded;
            return;
        }

        Context.ClearSearchFilter();
        Context.FileViewJumpTo(item.FilePath);
    }

    private void OnBreadcrumbItemPressed(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Item is not TreeItem treeItem) return;

        Context.ClearSearchFilter();
        Context.LoadFileItems(treeItem);
    }

    private void OnFileItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBox listBox) return;
        if (listBox.SelectedItem is not TreeItem item) return;

        FileItemDoubleTapped?.Invoke(item);

        if (item.Type == ENodeType.Folder)
        {
            ResetDragState();
            Context.LoadFileItems(item);
            item.Expanded = true;
        }
    }

    private void OnFlatItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: FlatItem item }) return;

        Context.FileViewJumpTo(item.Path);
    }

    private void OnItemRealized(object? sender, ItemRealizedEventArgs e)
    {
        if (e.Item is not TreeItem { FileBitmap: null } item) return;

        TaskService.Run(() => Context.RealizeFileDataAsync(item));
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (sender is not SearchBar searchBar) return;

        var text = searchBar.Text;

        if (Context.UseFlatView)
            Context.FlatSearchFilter = text;
        else
            Context.FileSearchFilter = text;
    }

    private void OnFlatViewHyperlinkPressed(object? sender, PointerPressedEventArgs e)
    {
        var searchTerm = Context.FileSearchFilter;

        Context.FileSearchFilter = string.Empty;
        Context.FileSearchText = string.Empty;

        Context.UseFlatView = true;
        Context.FlatSearchFilter = searchTerm;
        Context.FlatSearchText = searchTerm;
    }

    private void OnVfsFilterFlyoutOpened(object? sender, EventArgs e)
    {
        if (sender is not Flyout { Content: ListBox listBox }) return;
        SyncVfsFilterListBoxSelection(listBox);
    }

    private void OnVfsFilterSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncingVfsFilterSelection) return;

        foreach (var item in e.AddedItems.OfType<VfsFilterItem>())
            item.IsChecked = true;

        foreach (var item in e.RemovedItems.OfType<VfsFilterItem>())
            item.IsChecked = false;
    }

    private void SyncVfsFilterListBoxSelection(ListBox listBox)
    {
        _syncingVfsFilterSelection = true;
        try
        {
            listBox.SelectedItems?.Clear();
            foreach (var item in Context.VfsFilterCollection.Where(x => x.IsChecked))
                listBox.SelectedItems?.Add(item);
        }
        finally
        {
            _syncingVfsFilterSelection = false;
        }
    }

}