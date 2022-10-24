using System.Windows;
using System.Windows.Controls;

namespace FortnitePorting.Views.Controls;

public partial class NumericSlider
{
    public static readonly DependencyProperty LabelProperty = 
        DependencyProperty.Register(
            nameof(Label),
            typeof(string), 
            typeof(NumericSlider)
        );

    public string Label
    {
        get => (string) GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
    
    public static readonly DependencyProperty ValueProperty = 
        DependencyProperty.Register(
            nameof(Value),
            typeof(float), 
            typeof(NumericSlider)
        );

    public float Value
    {
        get => (float) GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    public NumericSlider()
    {
        InitializeComponent();
    }
}