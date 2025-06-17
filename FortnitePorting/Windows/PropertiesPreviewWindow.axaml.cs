using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Properties;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class PropertiesPreviewWindow : WindowBase<PropertiesPreviewWindowModel>
{
    public static PropertiesPreviewWindow? Instance;
    
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
    }

    public static void Preview(string name, string json)
    {
        if (Instance is null)
        {
            Instance = new PropertiesPreviewWindow();
            Instance.Show();
            Instance.BringToTop();
        }

        if (Instance.WindowModel.Assets.FirstOrDefault(asset => asset.AssetName.Equals(name)) is { } existing)
        {
            Instance.WindowModel.SelectedAsset = existing;
            return;
        }
        
        Instance.WindowModel.Assets.Add(new PropertiesContainer
        {
            AssetName = name,
            PropertiesData = json
        });
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