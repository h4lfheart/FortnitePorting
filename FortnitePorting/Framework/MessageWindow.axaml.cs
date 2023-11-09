using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FortnitePorting.Services;

namespace FortnitePorting.Framework;

public partial class MessageWindow : Window
{
    public static MessageWindow? ActiveWindow;
    private event EventHandler OnClosedWindow;
    public MessageWindow(string caption, string text, Window? owner = null, Action<object?, EventArgs>? onClosed = null)
    {
        InitializeComponent();
        DataContext = AppVM;

        Title = caption;
        CaptionTextBlock.Text = caption;
        InfoTextBlock.Text = text;
        Owner = owner;
        if (onClosed is not null) OnClosedWindow += (sender, args) => onClosed(sender, args);
    }

    public static void Show(string caption, string text, Window? owner = null, Action<object?, EventArgs>? onClosed = null)
    {
        TaskService.RunDispatcher(() =>
        {
            ActiveWindow?.Close(); // only keep one instance
            ActiveWindow = new MessageWindow(caption, text, owner, onClosed);
            ActiveWindow.Show();
        });
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
    
    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnContinueClicked(object? sender, RoutedEventArgs e)
    {
        Close();
        OnClosedWindow?.Invoke(this, EventArgs.Empty);
    }
}