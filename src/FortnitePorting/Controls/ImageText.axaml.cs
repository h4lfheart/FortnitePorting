using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace FortnitePorting.Controls;

public partial class ImageText : UserControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<ImageText, string>(nameof(Text), string.Empty);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<IImage> ImageProperty = AvaloniaProperty.Register<ImageText, IImage>(nameof(Image));

    public IImage Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public static readonly StyledProperty<int> ImageSizeProperty = AvaloniaProperty.Register<ImageText, int>(nameof(ImageSize), 20);

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

public class ImageTextExtension(string text, IImage image) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new ImageText
        {
            Image = image,
            Text = text
        };
    }
}