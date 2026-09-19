using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class ApplicationSettingsView : ViewBase<ApplicationSettingsViewModel>
{
    public ApplicationSettingsView() : base(AppSettings.Application)
    {
        InitializeComponent();
    }
}