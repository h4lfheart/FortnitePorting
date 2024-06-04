using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;
using BlenderSettingsViewModel = FortnitePorting.ViewModels.Settings.BlenderSettingsViewModel;

namespace FortnitePorting.Views.Settings;

public partial class ApplicationSettingsView : ViewBase<ApplicationSettingsViewModel>
{
    public ApplicationSettingsView() : base(AppSettings.Current.Application)
    {
        InitializeComponent();
    }
}