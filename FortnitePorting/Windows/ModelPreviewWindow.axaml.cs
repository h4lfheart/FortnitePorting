using System;
using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Framework;
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
    }

    public static void Preview(IEnumerable<UObject> objects)
    {
        // TODO model loading
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        Instance = null;
    }
}
