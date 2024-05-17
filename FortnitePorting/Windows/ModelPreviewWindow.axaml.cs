using System;
using Avalonia.Controls;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.OpenGL;
using FortnitePorting.OpenGL.Rendering;
using FortnitePorting.Services;
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
        Owner = ApplicationService.Application.MainWindow;
    }
    
    public ModelPreviewWindow(string name, UObject obj) : this()
    {
        ViewModel.MeshName = name;
        ViewModel.ViewerControl = new ModelViewerTkOpenGlControl(obj);
    }

    public static void Preview(UObject obj)
    {
        Preview(obj.Name, obj);
    }
    

    public static void Preview(string name, UObject obj)
    {
        if (Instance is not null && RenderManager.Instance is not null)
        {
            Instance.ViewModel.MeshName = name;
            Instance.ViewModel.ViewerControl.QueuedObject = obj;
            Instance.BringToTop();
            return;
        }
        
        TaskService.RunDispatcher(() =>
        {
            Instance = new ModelPreviewWindow(name, obj);
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