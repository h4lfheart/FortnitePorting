using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class SettingsView : ViewBase<SettingsViewModel>
{
    public SettingsView()
    {
        InitializeComponent();
    }
}