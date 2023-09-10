using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace FortnitePorting.Application;

public partial class AppWindow : Window
{
    public AppWindow()
    {
        InitializeComponent();
        this.AttachDevTools();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
}