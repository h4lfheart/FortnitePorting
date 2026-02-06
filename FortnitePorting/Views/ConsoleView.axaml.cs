using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FortnitePorting.Framework;
using FortnitePorting.Models.Information;
using FortnitePorting.ViewModels;
using ScottPlot.DataViews;

namespace FortnitePorting.Views;

public partial class ConsoleView : ViewBase<ConsoleViewModel>
{
    public ConsoleView()
    {
        InitializeComponent();

        ViewModel.Scroll = Scroll;
    }

    private async void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not FPLogEvent logEvent) return;

        await App.Clipboard.SetTextAsync(logEvent.Message);
        
        Info.Message("Console", "Copied log message to the clipboard!");
    }
}