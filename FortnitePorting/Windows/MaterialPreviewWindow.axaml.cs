using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using FortnitePorting.Framework;
using FortnitePorting.Models.Nodes.Material;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class MaterialPreviewWindow : NodeGraphPreviewWindowBase<MaterialPreviewWindow, MaterialPreviewWindowModel, MaterialNodeTree>
{
    public MaterialPreviewWindow()
    {
        InitializeComponent();
    }

    public static void Preview(UObject obj)
    {
        var window = GetOrCreate(() => new MaterialPreviewWindow());

        if (window.WindowModel.Trees.FirstOrDefault(mat => mat.Asset?.Name.Equals(obj.Name) ?? false) is
            { } existing)
        {
            window.WindowModel.SelectedTree = existing;
            return;
        }
        
        window.WindowModel.Load(obj);
    }

    private void OnNodePressed(object? sender, PointerPressedEventArgs e)
    {
        IsNodePress = true;
        
        if (e.ClickCount != 2) return;
        if (sender is not Control control) return;
        if (control.DataContext is not MaterialNode node) return;

        if (node.Package is not null && !node.Package.IsNull)
        {
            var package = node.Package.Load();
            switch (package)
            {
                case UMaterial material:
                {
                    Preview(material);
                    break;
                }
                case UMaterialFunction materialFunction:
                {
                    Preview(materialFunction);
                    break;
                }
            }
        }

        if (node.Subgraph is not null)
        {
            WindowModel.Load(node.Subgraph as MaterialNodeTree);
        }

        FocusLinkedNode(node);
    }
}
