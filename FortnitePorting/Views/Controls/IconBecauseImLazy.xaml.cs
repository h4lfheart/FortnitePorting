using System.Windows;
using System.Windows.Media;

namespace FortnitePorting.Views.Controls;

public partial class IconBecauseImLazy
{
    public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(IconBecauseImLazy));

    public ImageSource ImageSource
    {
        get => (ImageSource) GetValue(ImageSourceProperty);
        set => SetValue(ImageSourceProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(int), typeof(IconBecauseImLazy), new PropertyMetadata(24));

    public int IconSize
    {
        get => (int) GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public IconBecauseImLazy()
    {
        InitializeComponent();
    }
}