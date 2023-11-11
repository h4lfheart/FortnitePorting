using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using FortnitePorting.Application;

namespace FortnitePorting.Controls.Avalonia;

[PseudoClasses(":normal")]
public class AnimatableTabControl : TabControl
{
    public event EventHandler OnContentChanged;
    
    protected override Type StyleKeyOverride { get; } = typeof(TabControl);

    public AnimatableTabControl()
    {
        PseudoClasses.Add(":normal");
        OnContentChanged += AnimateChanged;
        this.GetObservable(SelectedContentProperty).Subscribe(obj => OnContentChanged.Invoke(SelectedContent, EventArgs.Empty));
    }
    
    private void AnimateChanged(object? obj, EventArgs e)
    {
        if (!AnimateOnChange) return;
        
        PseudoClasses.Remove(":normal");
        PseudoClasses.Add(":normal");
    }

    public bool AnimateOnChange => AppSettings.Current.UseTabTransition;
}