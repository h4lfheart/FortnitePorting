using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class InstallationSettingsView : ViewBase<InstallationSettingsViewModel>
{
    public InstallationSettingsView() : base(AppSettings.Current.Installation)
    {
        InitializeComponent();
    }
}