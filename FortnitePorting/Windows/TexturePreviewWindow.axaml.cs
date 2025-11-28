using System;
using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Viewers;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class TexturePreviewWindow : WindowBase<TexturePreviewWindowModel>
{
    public static TexturePreviewWindow? Instance;
    
    public TexturePreviewWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;
    }

    public static void Preview(string name, UTexture texture)
    {
        if (Instance is null)
        {
            Instance = new TexturePreviewWindow();
            Instance.Show();
        }
        
        Instance.BringToTop();

        if (Instance.WindowModel.Textures.FirstOrDefault(texture => texture.TextureName.Equals(name)) is { } existing)
        {
            Instance.WindowModel.SelectedTexture = existing;
            return;
        }

        var container = new TextureContainer
        {
            TextureName = name,
            Texture = texture
        };
        
        container.Update();
        
        Instance.WindowModel.Textures.Add(container);
        Instance.WindowModel.SelectedTexture = container;
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance = null;
    }
    
    private void OnTabClosed(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        if (args.Item is not TextureContainer texture) return;

        WindowModel.Textures.Remove(texture);

        if (WindowModel.Textures.Count == 0)
        {
            Close();
        }
    }
}