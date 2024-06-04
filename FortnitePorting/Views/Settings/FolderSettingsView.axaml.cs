using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class FolderSettingsView : ViewBase<FolderSettingsViewModel>
{
    public FolderSettingsView() : base(AppSettings.Current.ExportSettings.Folder)
    {
        InitializeComponent();
    }
}