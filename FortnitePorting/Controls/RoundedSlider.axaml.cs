using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace FortnitePorting.Controls;

public partial class RoundedSlider : UserControl
{
    public static readonly StyledProperty<float> ValueProperty = AvaloniaProperty.Register<RoundedSlider, float>(nameof(Value), defaultValue: 0.0f);
    public float Value
    {
        get => MathF.Round(GetValue(ValueProperty), Decimals);
        set => SetValue(ValueProperty, MathF.Round(value, Decimals));
    }
    
    public static readonly StyledProperty<int> DecimalsProperty = AvaloniaProperty.Register<RoundedSlider, int>(nameof(Decimals), defaultValue: 2);
    public int Decimals
    {
        get => GetValue(DecimalsProperty);
        set => SetValue(DecimalsProperty, value);
    }
    
    public RoundedSlider()
    {
        InitializeComponent();
    }
    
}