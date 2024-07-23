using System;
using System.Collections.Generic;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
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
        DataContext = WindowModel;
        Owner = ApplicationService.Application.MainWindow;
    }

    public static void Preview(IEnumerable<UObject> objects)
    {
        if (Instance is not null)
        {
            Instance.WindowModel.MeshName = string.Empty;
            Instance.WindowModel.ViewerControl.Context.QueuedObjects.AddRange(objects);
            Instance.BringToTop();
            return;
        }
        
        TaskService.RunDispatcher(() =>
        {
            Instance = new ModelPreviewWindow();
            Instance.WindowModel.MeshName = string.Empty;
            Instance.WindowModel.QueuedObjects = [..objects];
            Instance.Show();
            Instance.BringToTop();
        });
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance.WindowModel.ViewerControl.Context.Close();
        Instance = null;
    }
}
