using Avalonia;
using Avalonia.Controls;
using Material.Icons;

namespace FortnitePorting.Controls;

public partial class SettingBox : UserControl
{
    public static readonly StyledProperty<MaterialIconKind> IconProperty = AvaloniaProperty.Register<SettingBox, MaterialIconKind>(nameof(Icon), defaultValue: MaterialIconKind.Folder);
    public MaterialIconKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    
    public static readonly StyledProperty<string> PathProperty = AvaloniaProperty.Register<SettingBox, string>(nameof(Path), defaultValue: "Value");
    public string Path
    {
        get => GetValue(PathProperty);
        set => SetValue(PathProperty, value);
    }
    
    public static readonly StyledProperty<string> DisplayNameProperty = AvaloniaProperty.Register<SettingBox, string>(nameof(DisplayName), defaultValue: "Property");
    public string DisplayName
    {
        get => GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public SettingBox()
    {
        InitializeComponent();
    }
}