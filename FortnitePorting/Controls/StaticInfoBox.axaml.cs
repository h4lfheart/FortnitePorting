using Avalonia;
using Avalonia.Controls;

namespace FortnitePorting.Controls;

public partial class StaticInfoBox : UserControl
{
    public static readonly StyledProperty<string> ImageProperty = AvaloniaProperty.Register<SettingBox, string>(nameof(Image));

    public string Image
    {
        get => GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public static readonly StyledProperty<string> DisplayNameProperty = AvaloniaProperty.Register<SettingBox, string>(nameof(DisplayName), "Property");

    public string DisplayName
    {
        get => GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public StaticInfoBox()
    {
        InitializeComponent();
    }
}