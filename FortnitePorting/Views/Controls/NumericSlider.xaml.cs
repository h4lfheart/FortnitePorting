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
            typeof(double), 
            typeof(NumericSlider)
        );

    public double Value
    {
        get => (double) GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    public NumericSlider()
    {
        InitializeComponent();
    }
}