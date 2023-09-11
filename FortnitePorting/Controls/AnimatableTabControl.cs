using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
namespace FortnitePorting.Controls;

[PseudoClasses(":normal")]
public class AnimatableTabControl : TabControl
{
    protected override Type StyleKeyOverride { get; } = typeof(TabControl);

    public AnimatableTabControl()
    {
        PseudoClasses.Add(":normal");
        this.GetObservable(SelectedContentProperty).Subscribe(OnContentChanged);
    }

    private void OnContentChanged(object? obj)
    {
        if (!AnimateOnChange) return;
        
        PseudoClasses.Remove(":normal");
        PseudoClasses.Add(":normal");
    }

    public bool AnimateOnChange
    {
        get => GetValue(AnimateOnChangeProperty);
        set => SetValue(AnimateOnChangeProperty, value);
    }

    public static readonly StyledProperty<bool> AnimateOnChangeProperty =
        AvaloniaProperty.Register<AnimatableTabControl, bool>(nameof(AnimateOnChange), true);
}