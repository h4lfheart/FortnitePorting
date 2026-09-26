using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class AccountSettingsView : ViewBase<AccountSettingsViewModel>
{
    public AccountSettingsView() : base(AppSettings.Account)
    {
        InitializeComponent();
    }
}