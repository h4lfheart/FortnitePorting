using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Framework;
using FortnitePorting.Installer.ViewModels;

namespace FortnitePorting.Installer.Views;

public partial class MainView : ViewBase<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }
}