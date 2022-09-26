using System.Windows;
using System.Windows.Media;

namespace FortnitePorting.Views.Controls;

public partial class TextWithIcon
{
    public static readonly DependencyProperty ImageSourceProperty = 
        DependencyProperty.Register(
            nameof(ImageSource),
            typeof(ImageSource), 
            typeof(AssetSelector)
        );

    public ImageSource ImageSource
    {
        get => (ImageSource) GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }
    
    public static readonly DependencyProperty LabelProperty = 
        DependencyProperty.Register(
            nameof(Label),
            typeof(string), 
            typeof(AssetSelector)
        );

    public string Label
    {
        get => (string) GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
    public TextWithIcon()
    {
        InitializeComponent();
    }
}