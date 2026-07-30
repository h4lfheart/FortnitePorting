using Avalonia;
using Avalonia.Controls;

namespace FortnitePorting.Controls.Plugin;

public partial class PluginInstallationsHeader : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<PluginInstallationsHeader, string>(nameof(Title), string.Empty);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly StyledProperty<string> SubtitleProperty =
        AvaloniaProperty.Register<PluginInstallationsHeader, string>(nameof(Subtitle), string.Empty);

    public string Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public static readonly StyledProperty<object?> AddButtonContentProperty =
        AvaloniaProperty.Register<PluginInstallationsHeader, object?>(nameof(AddButtonContent));

    public object? AddButtonContent
    {
        get => GetValue(AddButtonContentProperty);
        set => SetValue(AddButtonContentProperty, value);
    }

    public PluginInstallationsHeader()
    {
        InitializeComponent();
    }
}
