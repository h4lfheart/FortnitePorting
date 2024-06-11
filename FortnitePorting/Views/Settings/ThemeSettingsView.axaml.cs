using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;
using BlenderSettingsViewModel = FortnitePorting.ViewModels.Settings.BlenderSettingsViewModel;

namespace FortnitePorting.Views.Settings;

public partial class ThemeSettingsView : ViewBase<ThemeSettingsViewModel>
{
    public ThemeSettingsView() : base(AppSettings.Current.Theme)
    {
        InitializeComponent();
    }
}