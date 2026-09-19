using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class SettingsView : ViewBase<SettingsViewModel>
{
    public SettingsView()
    {
        InitializeComponent();
        
        Navigation.Settings.Initialize(Sidebar, ContentFrame);
    }

    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        Navigation.Settings.Open(e.Tag);
    }
}