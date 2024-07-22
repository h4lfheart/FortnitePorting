using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Installer.ViewModels;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.Installer.Views;

public partial class InstallView : ViewBase<InstallViewModel>
{
    public InstallView()
    {
        InitializeComponent();
    }
}