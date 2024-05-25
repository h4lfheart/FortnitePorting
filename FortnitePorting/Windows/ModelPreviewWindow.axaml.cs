using System;
using Avalonia.Controls;
using Avalonia.Platform;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.OpenGL;
using FortnitePorting.OpenGL.Rendering;
using FortnitePorting.Services;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace FortnitePorting.Windows;

public partial class ModelPreviewWindow : WindowBase<ModelPreviewViewModel>
{
    public ModelPreviewWindow()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Owner = ApplicationService.Application.MainWindow;
    }

    public static void Preview(UObject obj)
    {
        Preview(obj.Name, obj);
    }

    public static void Preview(string name, UObject obj)
    {
        TaskService.RunDispatcher(() =>
        {
            if (ModelViewerWindow.Instance is null)
            {
                ModelViewerWindow.Instance ??= new ModelViewerWindow(GameWindowSettings.Default, new NativeWindowSettings
                {
                    ClientSize = new Vector2i(960, 540),
                    NumberOfSamples = 32,
                    WindowBorder = WindowBorder.Resizable,
                    APIVersion = new Version(4, 6),
                    Title = "Model Viewer",
                    StartVisible = false
                });

                ModelViewerWindow.Instance.QueuedObject = obj;
                ModelViewerWindow.Instance.Run();
            }
            else
            {
                ModelViewerWindow.Instance.QueuedObject = obj;
            }
        });
    }
}
