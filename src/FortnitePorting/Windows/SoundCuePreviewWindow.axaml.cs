using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using CUE4Parse.UE4.Assets.Exports;
using FortnitePorting.Framework;
using FortnitePorting.Models.Nodes.SoundCue;
using FortnitePorting.WindowModels;
using Node = FortnitePorting.Models.Nodes.Node;

namespace FortnitePorting.Windows;

public partial class SoundCuePreviewWindow : NodeGraphPreviewWindowBase<SoundCuePreviewWindow, SoundCuePreviewWindowModel, SoundCueNodeTree>
{
    public SoundCuePreviewWindow()
    {
        InitializeComponent();
    }

    public static void Preview(UObject obj)
    {
        var window = WindowManager.GetOrShowPreview(() => new SoundCuePreviewWindow());

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
        if (control.DataContext is not Node node) return;
        
        FocusLinkedNode(node);
    }
}
