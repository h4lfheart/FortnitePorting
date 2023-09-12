using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Material.Icons;

namespace FortnitePorting.Controls;

public partial class MaterialIconText : UserControl
{
    public static readonly StyledProperty<string> TextProperty = AvaloniaProperty.Register<MaterialIconText, string>(nameof(Text), defaultValue: "String");
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
    
    public static readonly StyledProperty<MaterialIconKind> IconProperty = AvaloniaProperty.Register<MaterialIconText, MaterialIconKind>(nameof(Icon), defaultValue: MaterialIconKind.Folder);
    public MaterialIconKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    
    public MaterialIconText()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}