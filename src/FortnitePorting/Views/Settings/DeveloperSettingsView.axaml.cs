using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class DeveloperSettingsView : ViewBase<DeveloperSettingsViewModel>
{
    public DeveloperSettingsView() : base(AppSettings.Developer)
    {
        InitializeComponent();
    }
}