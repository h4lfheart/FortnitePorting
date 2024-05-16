using System;
using Avalonia.Controls;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.OpenGL;
using FortnitePorting.OpenGL.Rendering;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Windows;

public partial class ModelPreviewWindow : WindowBase<ModelPreviewViewModel>
{
    public static ModelPreviewWindow? Instance;
    
    public ModelPreviewWindow()
    {
        InitializeComponent();
        DataContext = ViewModel;
    }
    
    public ModelPreviewWindow(UObject obj) : this()
    {
        ViewModel.MeshName = obj.Name;
        ViewModel.ViewerControl = new ModelViewerTkOpenGlControl(obj);
    }

    public static void Preview(UObject obj)
    {
        if (Instance is not null && RenderManager.Instance is not null)
        {
            Instance.ViewModel.MeshName = obj.Name;
            Instance.ViewModel.ViewerControl.QueuedObject = obj;
            return;
        }
        
        TaskService.RunDispatcher(() =>
        {
            Instance = new ModelPreviewWindow(obj);
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