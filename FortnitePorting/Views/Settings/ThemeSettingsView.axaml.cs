using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class ThemeSettingsView : ViewBase<ThemeSettingsViewModel>
{
    public ThemeSettingsView() : base(AppSettings.Theme)
    {
        InitializeComponent();
    }
}