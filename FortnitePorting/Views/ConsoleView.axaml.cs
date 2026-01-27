using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Framework;
using FortnitePorting.Models.Serilog;
using FortnitePorting.ViewModels;
using ScottPlot.DataViews;

namespace FortnitePorting.Views;

public partial class ConsoleView : ViewBase<ConsoleViewModel>
{
    public ConsoleView() : base(ConsoleVM)
    {
        InitializeComponent();
        ViewModel.Scroll = Scroll;
    }

    private void OnLogPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not FortnitePortingLogEvent logEvent) return;

        Clipboard.SetTextAsync(logEvent.LogString);
        AppWM.Message("Info", "Copied log to clipboard!");
    }
}