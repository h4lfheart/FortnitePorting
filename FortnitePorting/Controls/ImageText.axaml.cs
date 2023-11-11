using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Material.Icons;

namespace FortnitePorting.Controls;

public partial class ImageText : UserControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<ImageText, string>(nameof(Text), defaultValue: string.Empty);
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public static readonly StyledProperty<string> ImageProperty = AvaloniaProperty.Register<ImageText, string>(nameof(Image));
    public string Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }
    
    public static readonly StyledProperty<int> ImageSizeProperty = AvaloniaProperty.Register<ImageText, int>(nameof(ImageSize), defaultValue: 20);
    public int ImageSize
    {
        get => GetValue(ImageSizeProperty);
        set => SetValue(ImageSizeProperty, value);
    }
    
    public ImageText()
    {
        InitializeComponent();
    }
}