using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using BlenderSettingsViewModel = FortnitePorting.ViewModels.Settings.BlenderSettingsViewModel;

namespace FortnitePorting.Views.Settings;

public partial class BlenderSettingsView : ViewBase<BlenderSettingsViewModel>
{
    public BlenderSettingsView() : base(AppSettings.Current.ExportSettings.Blender)
    {
        InitializeComponent();
    }
}