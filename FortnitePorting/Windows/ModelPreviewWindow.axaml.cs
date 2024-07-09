using System;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.OpenGL;
using FortnitePorting.OpenGL.Rendering;
using FortnitePorting.Services;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using FortnitePorting.WindowModels;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace FortnitePorting.Windows;

public partial class ModelPreviewWindow : WindowBase<ModelPreviewWindowModel>
{
    public static ModelPreviewWindow? Instance;
    
    public ModelPreviewWindow()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Owner = ApplicationService.Application.MainWindow;
    }

    public static void Preview(string name, UObject obj)
    {
        if (Instance is not null)
        {
            Instance.ViewModel.MeshName = name;
            Instance.ViewModel.ViewerControl.Context.QueuedObject = obj;
            Instance.BringToTop();
            return;
        }
        
        TaskService.RunDispatcher(() =>
        {
            Instance = new ModelPreviewWindow();
            Instance.ViewModel.MeshName = name;
            Instance.ViewModel.QueuedObject = obj;
            Instance.Show();
            Instance.BringToTop();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance.ViewModel.ViewerControl.Context.Close();
        Instance = null;
    }
}
