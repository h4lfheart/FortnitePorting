using FortnitePorting.Launcher.Application;
using FortnitePorting.Launcher.ViewModels;
using FortnitePorting.Shared.Framework;

namespace FortnitePorting.Launcher.Views;

public partial class SettingsView : ViewBase<SettingsViewModel>
{
    public SettingsView() : base(AppSettings.Current, initializeViewModel: false)
    {
        InitializeComponent();
    }
}