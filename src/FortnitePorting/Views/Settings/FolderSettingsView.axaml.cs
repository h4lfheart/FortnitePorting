using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class FolderSettingsView : SettingsSectionViewBase<FolderSettingsViewModel>
{
    public FolderSettingsView() : base(AppSettings.ExportSettings.Folder)
    {
        InitializeComponent();
    }

    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        ApplySidebarSection(SectionContent, e);
    }
}
