using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Animation;
using FortnitePorting.Framework;
using FortnitePorting.Models.Viewers;
using FortnitePorting.WindowModels;

namespace FortnitePorting.Windows;

public partial class ModelPreviewWindow : PreviewWindowBase<ModelPreviewWindow, ModelPreviewWindowModel>
{
    public ModelPreviewWindow()
    {
        InitializeComponent();

        WindowModel.InitializeContext();

        ScrubSlider.AddHandler(PointerPressedEvent, OnScrubPointerPressed, RoutingStrategies.Tunnel);
        ScrubSlider.AddHandler(PointerReleasedEvent, OnScrubPointerReleased, RoutingStrategies.Tunnel);
        ScrubSlider.AddHandler(PointerCaptureLostEvent, OnScrubPointerCaptureLost, RoutingStrategies.Tunnel);
    }

    public static void Preview(IEnumerable<UObject> objects, UAnimationAsset? animation = null)
    {
        var window = WindowManager.GetOrShowPreview(() => new ModelPreviewWindow());
        window.WindowModel.LoadScene(objects, animation);
    }

    public static bool TryApplyAnimation(UAnimationAsset animation)
    {
        var existing = WindowManager.FindOpen<ModelPreviewWindow>();
        if (existing?.WindowModel is not { HasSkeletalMesh: true })
            return false;

        existing.WindowModel.ApplyAnimation(animation);
        existing.BringToTop();
        return true;
    }

    private void OnScrubPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        WindowModel.BeginScrub();
    }

    private void OnScrubPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        WindowModel.EndScrub();
    }

    private void OnScrubPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        WindowModel.EndScrub();
    }

    private void OnScrubValueChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if (sender is not Slider) return;
        WindowModel.ScrubTo((float) e.NewValue);
    }

    private void OnSectionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: AnimSectionItem section })
            return;

        WindowModel.JumpToSection(section.Name);
    }
}
