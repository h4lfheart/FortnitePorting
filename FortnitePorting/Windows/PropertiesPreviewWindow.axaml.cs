using System;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaEdit.Folding;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.AvaloniaEdit;
using FortnitePorting.WindowModels;
using PropertiesContainer = FortnitePorting.Models.Viewers.PropertiesContainer;

namespace FortnitePorting.Windows;

public partial class PropertiesPreviewWindow : WindowBase<PropertiesPreviewWindowModel>
{
    public static PropertiesPreviewWindow? Instance;
    private FoldingManager _foldingManager;
    private bool _isInitialized;
    
    public PropertiesPreviewWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        Editor.TextArea.TextView.BackgroundRenderers.Add(new IndentGuideLinesRenderer(Editor));
        Editor.TextArea.TextView.ElementGenerators.Add(new FilePathElementGenerator());
        
        _foldingManager = FoldingManager.Install(Editor.TextArea);
        StyleFoldingMargin();
        
        WindowModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(PropertiesPreviewWindowModel.SelectedAsset) && _isInitialized)
            {
                UpdateEditorContent();
            }
        };

        _isInitialized = true;
        
        if (WindowModel.SelectedAsset != null)
        {
            UpdateEditorContent();
        }
    }

    private void UpdateEditorContent()
    {
        if (WindowModel.SelectedAsset == null) return;

        Dispatcher.UIThread.Post(() =>
        {
            Editor.Document.Text = WindowModel.SelectedAsset.PropertiesData;
            JsonFoldingStrategy.UpdateFoldings(_foldingManager, Editor.Document);
        }, DispatcherPriority.Background);
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

    public static void Preview(string name, string json)
    {
        if (Instance == null)
        {
            Instance = new PropertiesPreviewWindow();
            Instance.Show();
            Instance.BringToTop();
        }
        
        Instance.Editor.ScrollToHome();

        var existing = Instance.WindowModel.Assets.FirstOrDefault(asset => asset.AssetName.Equals(name));
        if (existing != null)
        {
            Instance.WindowModel.SelectedAsset = existing;
            return;
        }

        var container = new PropertiesContainer
        {
            AssetName = name,
            PropertiesData = json
        };
        
        Instance.WindowModel.Assets.Add(container);
        Instance.WindowModel.SelectedAsset = container;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        Instance = null;
    }

    private void OnTabClosed(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is not PropertiesContainer properties) return;

        WindowModel.Assets.Remove(properties);

        if (WindowModel.Assets.Count == 0)
        {
            Close();
        }
    }
}
