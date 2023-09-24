using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace FortnitePorting.Framework;

public partial class MessageWindow : Window
{
    public MessageWindow(string caption, string text, Window? owner = null)
    {
        InitializeComponent();
        DataContext = AppVM;

        Title = caption;
        CaptionTextBlock.Text = caption;
        InfoTextBlock.Text = text;
        Owner = owner;
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }
    
    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}