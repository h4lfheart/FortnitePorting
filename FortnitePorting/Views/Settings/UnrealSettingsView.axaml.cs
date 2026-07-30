using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class UnrealSettingsView : SettingsSectionViewBase<UnrealSettingsViewModel>
{
    public UnrealSettingsView() : base(AppSettings.ExportSettings.Unreal)
    {
        InitializeComponent();
    }

    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        ApplySidebarSection(SectionContent, e);
    }
}
