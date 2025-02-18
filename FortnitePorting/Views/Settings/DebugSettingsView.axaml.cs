using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class DebugSettingsView : ViewBase<DebugSettingsViewModel>
{
    public DebugSettingsView() : base(AppSettings.Current.Debug)
    {
        InitializeComponent();
    }
}