using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Material.Icons;

namespace FortnitePorting.Shared.Controls;

public partial class IconText : UserControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<IconText, string>(nameof(Text), string.Empty);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<MaterialIconKind> IconProperty = AvaloniaProperty.Register<IconText, MaterialIconKind>(nameof(Icon));

    public MaterialIconKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly StyledProperty<int> IconSizeProperty = AvaloniaProperty.Register<IconText, int>(nameof(IconSize), 20);

    public int IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }
    
    public IconText()
    {
        InitializeComponent();
    }
}

public class IconTextExtension(string text, MaterialIconKind icon) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return new IconText
        {
            Icon = icon,
            Text = text
        };
    }
}