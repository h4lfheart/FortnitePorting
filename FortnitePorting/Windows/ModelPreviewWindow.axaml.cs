using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Framework;
using FortnitePorting.Rendering;
using FortnitePorting.Services;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class ModelPreviewWindow : WindowBase<ModelPreviewWindowModel>
{
    public static ModelPreviewWindow? Instance;
    
    public ModelPreviewWindow()
    {
        InitializeComponent();
        DataContext = WindowModel;
        Owner = App.Lifetime.MainWindow;

        WindowModel.Context ??= new ModelViewerContext();
        WindowModel.Control = new ModelPreviewControl(WindowModel.Context);
    }

    public static void Preview(IEnumerable<UObject> objects)
    {
        if (Instance is not null)
        {
            Instance.WindowModel.MeshName = string.Empty;
            Instance.WindowModel.LoadQueue(new Queue<UObject>(objects));
            Instance.BringToTop();
            return;
        }
        
        TaskService.RunDispatcher(() =>
        {
            Instance = new ModelPreviewWindow();
            Instance.WindowModel.MeshName = string.Empty;
            Instance.WindowModel.LoadQueue(new Queue<UObject>(objects));
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
