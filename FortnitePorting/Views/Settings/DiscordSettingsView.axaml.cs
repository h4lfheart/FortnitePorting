using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;
using BlenderSettingsViewModel = FortnitePorting.ViewModels.Settings.BlenderSettingsViewModel;

namespace FortnitePorting.Views.Settings;

public partial class DiscordSettingsView : ViewBase<DiscordSettingsViewModel>
{
    public DiscordSettingsView() : base(AppSettings.Current.Discord)
    {
        InitializeComponent();
    }
}