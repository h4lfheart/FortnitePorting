using System;
using Avalonia;
using Avalonia.Controls;

namespace FortnitePorting.Controls;

public partial class RoundedSlider : UserControl
{
    public static readonly StyledProperty<float> ValueProperty = AvaloniaProperty.Register<RoundedSlider, float>(nameof(Value));

    public float Value
    {
        get => MathF.Round(GetValue(ValueProperty), Decimals);
        set => SetValue(ValueProperty, MathF.Round(value, Decimals));
    }

    public static readonly StyledProperty<int> DecimalsProperty = AvaloniaProperty.Register<RoundedSlider, int>(nameof(Decimals), 2);

    public int Decimals
    {
        get => GetValue(DecimalsProperty);
        set => SetValue(DecimalsProperty, value);
    }
    
    public static readonly StyledProperty<float> MinimumProperty = AvaloniaProperty.Register<RoundedSlider, float>(nameof(Minimum), defaultValue: 0.0f);

    public float Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }
    
    public static readonly StyledProperty<float> MaximumProperty = AvaloniaProperty.Register<RoundedSlider, float>(nameof(Maximum), defaultValue: 1.0f);

    public float Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    
    public static readonly StyledProperty<float> TickFrequencyProperty = AvaloniaProperty.Register<RoundedSlider, float>(nameof(TickFrequency), defaultValue: 0.1f);

    public float TickFrequency
    {
        get => GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    
    public RoundedSlider()
    {
        InitializeComponent();
    }
}