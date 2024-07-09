using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class OnlineSettingsView : ViewBase<OnlineSettingsViewModel>
{
    public OnlineSettingsView() : base(AppSettings.Current.Online)
    {
        InitializeComponent();
    }
}