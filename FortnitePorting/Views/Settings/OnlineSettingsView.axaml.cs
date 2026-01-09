using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class OnlineSettingsView : ViewBase<OnlineSettingsViewModel>
{
    public OnlineSettingsView() : base(AppSettings.Online)
    {
        InitializeComponent();
    }
}