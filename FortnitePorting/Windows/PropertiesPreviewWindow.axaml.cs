using System;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Rendering;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.AvaloniaEdit;
using FortnitePorting.Services;
using FortnitePorting.WindowModels;
using PropertiesContainer = FortnitePorting.Models.Viewers.PropertiesContainer;

namespace FortnitePorting.Windows;

public partial class PropertiesPreviewWindow : PreviewWindowBase<PropertiesPreviewWindow, PropertiesPreviewWindowModel>
{
    private FoldingManager _foldingManager;
    private bool _isInitialized;
    private bool _isRestoringScroll;
    
    public PropertiesPreviewWindow()
    {
        InitializeComponent();
        
        WindowModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(PropertiesPreviewWindowModel.SelectedAsset) && _isInitialized)
            {
                UpdateEditorContent();
            }
        };
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        Editor.TextArea.TextView.ScrollOffsetChanged += OnScrollOffsetChanged;
        Editor.TextArea.TextView.BackgroundRenderers.Add(new IndentGuideLinesRenderer(Editor));
        Editor.TextArea.TextView.ElementGenerators.Add(new FilePathElementGenerator());
        
        _foldingManager = FoldingManager.Install(Editor.TextArea);
        StyleFoldingMargin();
        
        _isInitialized = true;
        
        if (WindowModel.SelectedAsset != null)
        {
            UpdateEditorContent();
        }
    }

    private void OnScrollOffsetChanged(object? sender, EventArgs e)
    {
        if (_isRestoringScroll) return;
        
        var line = Editor.TextArea.TextView.GetDocumentLineByVisualTop(Editor.TextArea.TextView.ScrollOffset.Y);
        if (line is null) return;
        
        WindowModel.SelectedAsset.ScrollLine = line.LineNumber;
    }

    private void UpdateEditorContent()
    {
        if (WindowModel.SelectedAsset == null) return;

        _isRestoringScroll = true;
        
        Editor.Document.Text = WindowModel.SelectedAsset.PropertiesData;
        JsonFoldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
        
        TaskService.RunDispatcher(() =>
        {
            Editor.ScrollTo(WindowModel.SelectedAsset.ScrollLine, 0, VisualYPosition.LineTop, 0, 0);

            _isRestoringScroll = false;
        }, DispatcherPriority.Render);
    }

    private void StyleFoldingMargin()
    {
        var margin = Editor.TextArea.LeftMargins.OfType<FoldingMargin>().FirstOrDefault();
        if (margin == null) return;

        margin.SetValue(FoldingMargin.FoldingMarkerBrushProperty, new SolidColorBrush(Color.Parse("#808081")));
        margin.SetValue(FoldingMargin.FoldingMarkerBackgroundBrushProperty, new SolidColorBrush(Color.Parse("#212121")));
        margin.SetValue(FoldingMargin.SelectedFoldingMarkerBrushProperty, new SolidColorBrush(Color.Parse("#D0D0D1")));
        margin.SetValue(FoldingMargin.SelectedFoldingMarkerBackgroundBrushProperty, new SolidColorBrush(Color.Parse("#212121")));
    }

    public static void Preview(string name, string json, int targetIndex = -1)
    {
        var window = WindowManager.GetOrShowPreview(() => new PropertiesPreviewWindow());

        var targetLine = 0;
        if (targetIndex >= 0)
        {
            targetLine = StringExtensions.GetPropertiesExportIndexLine(json, targetIndex);
            window.Editor.ScrollTo(targetLine, 0, VisualYPosition.LineTop, 0, 0);
        }

        var existing = window.WindowModel.Assets.FirstOrDefault(asset => asset.AssetName.Equals(name));
        if (existing != null)
        {
            window.WindowModel.SelectedAsset = existing;
            window.WindowModel.SelectedAsset.ScrollLine = targetLine;
            return;
        }
        
        var container = new PropertiesContainer
        {
            AssetName = name,
            PropertiesData = json,
            ScrollLine = targetLine
        };
        
        window.WindowModel.Assets.Add(container);
        window.WindowModel.SelectedAsset = container;
    }

    private void OnTabClosed(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        RemoveTabAndCloseIfEmpty(WindowModel.Assets, args.Item);
    }
}
