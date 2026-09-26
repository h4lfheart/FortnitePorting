using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class BlenderSettingsView : SettingsSectionViewBase<BlenderSettingsViewModel>
{
    public BlenderSettingsView() : base(AppSettings.ExportSettings.Blender)
    {
        InitializeComponent();
    }

    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        ApplySidebarSection(SectionContent, e);
    }
}
