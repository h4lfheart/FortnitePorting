using System.Linq;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Viewers;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class TexturePreviewWindow : PreviewWindowBase<TexturePreviewWindow, TexturePreviewWindowModel>
{
    public TexturePreviewWindow()
    {
        InitializeComponent();
    }

    public static void Preview(string name, UTexture texture)
    {
        var window = WindowManager.GetOrShowPreview(() => new TexturePreviewWindow());

        if (window.WindowModel.Textures.FirstOrDefault(texture => texture.TextureName.Equals(name)) is { } existing)
        {
            window.WindowModel.SelectedTexture = existing;
            return;
        }

        var container = new TextureContainer
        {
            TextureName = name,
            Texture = texture
        };
        
        container.Update();
        
        window.WindowModel.Textures.Add(container);
        window.WindowModel.SelectedTexture = container;
    }
    
    private void OnTabClosed(TabView sender, TabViewTabCloseRequestedEventArgs args)
    {
        RemoveTabAndCloseIfEmpty(WindowModel.Textures, args.Item);
    }
}
