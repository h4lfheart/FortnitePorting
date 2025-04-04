using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class BlenderSettingsView : ViewBase<BlenderSettingsViewModel>
{
    public BlenderSettingsView() : base(AppSettings.Current.ExportSettings.Blender)
    {
        InitializeComponent();
    }
}