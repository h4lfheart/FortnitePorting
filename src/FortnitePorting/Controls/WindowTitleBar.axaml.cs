using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace FortnitePorting.Controls;

public partial class WindowTitleBar : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<WindowTitleBar, string?>(nameof(Title));

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public static readonly StyledProperty<bool> ShowMaximizeProperty =
        AvaloniaProperty.Register<WindowTitleBar, bool>(nameof(ShowMaximize), true);

    public bool ShowMaximize
    {
        get => GetValue(ShowMaximizeProperty);
        set => SetValue(ShowMaximizeProperty, value);
    }

    public WindowTitleBar()
    {
        InitializeComponent();
    }

    private Window? HostWindow => TopLevel.GetTopLevel(this) as Window;

    private void OnPointerPressedUpperBar(object? sender, PointerPressedEventArgs e)
    {
        HostWindow?.BeginMoveDrag(e);
    }

    private void OnMinimizePressed(object? sender, PointerPressedEventArgs e)
    {
        HostWindow?.WindowState = WindowState.Minimized;
    }

    private void OnMaximizePressed(object? sender, PointerPressedEventArgs e)
    {
        HostWindow?.WindowState = HostWindow.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void OnClosePressed(object? sender, PointerPressedEventArgs e)
    {
        HostWindow?.Close();
    }
}
