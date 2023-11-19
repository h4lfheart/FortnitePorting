using Avalonia;
using Avalonia.Controls;
using Material.Icons;

namespace FortnitePorting.Controls;

public partial class SettingBox : UserControl
{
    public static readonly StyledProperty<MaterialIconKind> IconProperty = AvaloniaProperty.Register<SettingBox, MaterialIconKind>(nameof(Icon), MaterialIconKind.Folder);

    public MaterialIconKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly StyledProperty<string> PathProperty = AvaloniaProperty.Register<SettingBox, string>(nameof(Path), "???");

    public string Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }

    public static readonly StyledProperty<string> DisplayNameProperty = AvaloniaProperty.Register<SettingBox, string>(nameof(DisplayName), "Property");

    public string DisplayName
    {
        get => GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public static readonly StyledProperty<bool> ShowIconProperty = AvaloniaProperty.Register<SettingBox, bool>(nameof(ShowIcon), true);

    public bool ShowIcon
    {
        get => GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    public SettingBox()
    {
        InitializeComponent();
    }
}