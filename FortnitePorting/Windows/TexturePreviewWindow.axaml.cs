using System;
using Avalonia.Controls;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.Services;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class TexturePreviewWindow : WindowBase<TexturePreviewWindowModel>
{
    public static TexturePreviewWindow? Instance;
    
    public TexturePreviewWindow(string name, UTexture texture)
    {
        InitializeComponent();
        DataContext = ViewModel;
        Owner = ApplicationService.Application.MainWindow;

        ViewModel.TextureName = name;
        ViewModel.Texture = texture;
        ViewModel.Update();
    }

    public static void Preview(string name, UTexture texture)
    {
        if (Instance is not null)
        {
            Instance.ViewModel.TextureName = name;
            Instance.ViewModel.Texture = texture;
            Instance.ViewModel.Update();
            Instance.BringToTop();
            return;
        }

        TaskService.RunDispatcher(() =>
        {
            Instance = new TexturePreviewWindow(name, texture);
            Instance.Show();
            Instance.BringToTop();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance = null;
    }
}