using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FortnitePorting.Framework;
using FortnitePorting.Models.Information;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ConsoleView : ViewBase<ConsoleViewModel>
{
    private bool _didInitialScroll = false;

    public ConsoleView()
    {
        InitializeComponent();
        ViewModel.Scroll = Scroll;

        Scroll.LayoutUpdated += (sender, args) =>
        {
            if (_didInitialScroll) return;
            Scroll.ScrollToEnd();
            _didInitialScroll = true;
        };
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        Scroll.ScrollToEnd();
        _didInitialScroll = false;
    }
    
    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not FPLogEvent logEvent) return;

        await App.Clipboard.SetTextAsync(logEvent.Message);
        
        Info.Message("Console", "Copied log message to the clipboard!");
    }
}