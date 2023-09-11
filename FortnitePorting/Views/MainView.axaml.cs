using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class MainView : ViewBase<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }

}