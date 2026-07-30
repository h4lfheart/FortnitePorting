using System.Collections.Generic;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Framework;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class ModelPreviewWindow : PreviewWindowBase<ModelPreviewWindow, ModelPreviewWindowModel>
{
    public ModelPreviewWindow()
    {
        InitializeComponent();
        
        WindowModel.InitializeContext();
    }

    public static void Preview(IEnumerable<UObject> objects)
    {
        var window = WindowManager.GetOrShowPreview(() => new ModelPreviewWindow());
        window.WindowModel.LoadScene(objects);
    }
}
