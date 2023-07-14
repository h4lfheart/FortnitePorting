using System.Windows;
using System.Windows.Media;

namespace FortnitePorting.Views.Controls;

public partial class TextWithIcon
{
    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(TextWithIcon));

    public ImageSource ImageSource
    {
        get => (ImageSource) GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(int), typeof(TextWithIcon), new PropertyMetadata(24));

    public int IconSize
    {
        get => (int) GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty LabelFontSizeProperty = DependencyProperty.Register(nameof(LabelFontSize), typeof(int), typeof(TextWithIcon));

    public int LabelFontSize
    {
        get => (int) GetValue(LabelFontSizeProperty);
        set => SetValue(LabelFontSizeProperty, value);
    }

    public static readonly DependencyProperty LabelFontWeightProperty = DependencyProperty.Register(nameof(LabelFontWeight), typeof(FontWeight), typeof(TextWithIcon), new PropertyMetadata(FontWeights.Normal));

    public FontWeight LabelFontWeight
    {
        get => (FontWeight) GetValue(LabelFontWeightProperty);
        set => SetValue(LabelFontWeightProperty, value);
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(TextWithIcon));

    public string Label
    {
        get => (string) GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public TextWithIcon()
    {
        InitializeComponent();
    }

    public TextWithIcon(bool isProp = false)
    {
        InitializeComponent();
    }
}