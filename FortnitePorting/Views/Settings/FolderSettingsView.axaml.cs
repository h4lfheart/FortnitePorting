using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class FolderSettingsView : ViewBase<FolderSettingsViewModel>
{
    public FolderSettingsView() : base(AppSettings.ExportSettings.Folder)
    {
        InitializeComponent();
    }
}