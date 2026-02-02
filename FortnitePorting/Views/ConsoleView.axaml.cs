using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FortnitePorting.Framework;
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
}