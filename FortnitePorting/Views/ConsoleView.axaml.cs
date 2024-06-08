using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Models.Serilog;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ConsoleView : ViewBase<ConsoleViewModel>
{
    public ConsoleView()
    {
        InitializeComponent();
        ViewModel.Scroll = Scroll;
    }

    private void OnLogPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not FortnitePortingLogEvent logEvent) return;

        Clipboard.SetTextAsync(logEvent.LogString);
        AppVM.Message("Info", "Copied log to clipboard!");
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        Scroll.ScrollToEnd();
    }
}