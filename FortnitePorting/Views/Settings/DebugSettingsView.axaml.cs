using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class DebugSettingsView : ViewBase<DebugSettingsViewModel>
{
    public DebugSettingsView() : base(AppSettings.Debug)
    {
        InitializeComponent();
    }
}