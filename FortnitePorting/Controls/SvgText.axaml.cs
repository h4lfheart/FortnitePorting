using Avalonia;
using Avalonia.Controls;

namespace FortnitePorting.Controls;

public partial class SvgText : UserControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<SvgText, string>(nameof(Text), string.Empty);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<string> ImageProperty = AvaloniaProperty.Register<SvgText, string>(nameof(Image));

    public string Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public static readonly StyledProperty<int> ImageSizeProperty = AvaloniaProperty.Register<SvgText, int>(nameof(ImageSize), 20);

    public int ImageSize
    {
        get => GetValue(ImageSizeProperty);
        set => SetValue(ImageSizeProperty, value);
    }

    public SvgText()
    {
        InitializeComponent();
    }
}