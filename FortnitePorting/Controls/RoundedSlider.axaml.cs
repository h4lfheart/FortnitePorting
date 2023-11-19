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

    public RoundedSlider()
    {
        InitializeComponent();
    }
}