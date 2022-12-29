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
    
    public static readonly DependencyProperty MaximumProperty = 
        DependencyProperty.Register(
            nameof(Maximum),
            typeof(double), 
            typeof(NumericSlider),
            new PropertyMetadata(1.0)
        );

    public double Maximum
    {
        get => (double) GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }
    
    public static readonly DependencyProperty StepSizeProperty = 
        DependencyProperty.Register(
            nameof(StepSize),
            typeof(double), 
            typeof(NumericSlider),
            new PropertyMetadata(0.1)
        );

    public double StepSize
    {
        get => (double) GetValue(StepSizeProperty);
        set => SetValue(StepSizeProperty, value);
    }
    
    public static readonly DependencyProperty SnapProperty = 
        DependencyProperty.Register(
            nameof(Snap),
            typeof(bool), 
            typeof(NumericSlider),
            new PropertyMetadata(false)
        );

    public bool Snap
    {
        get => (bool) GetValue(SnapProperty);
        set => SetValue(SnapProperty, value);
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