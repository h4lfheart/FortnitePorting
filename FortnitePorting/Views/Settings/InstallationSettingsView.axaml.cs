using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;
using BlenderSettingsViewModel = FortnitePorting.ViewModels.Settings.BlenderSettingsViewModel;

namespace FortnitePorting.Views.Settings;

public partial class InstallationSettingsView : ViewBase<InstallationSettingsViewModel>
{
    public InstallationSettingsView() : base(AppSettings.Current.Installation)
    {
        InitializeComponent();
    }
}