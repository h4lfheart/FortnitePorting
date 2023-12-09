using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace FortnitePorting.Installer.Application;

public partial class AppWindow : Window
{
    public AppWindow()
    {
        InitializeComponent();
        DataContext = AppVM;
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
    
    private void OnMinimizeClicked(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Shutdown();
    }
}