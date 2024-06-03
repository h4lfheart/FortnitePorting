using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.ExportSettings;

namespace FortnitePorting.Views.ExportSettings;

public partial class FolderExportSettingsView : ViewBase<FolderExportSettingsViewModel>
{
    public FolderExportSettingsView() : base(AppSettings.Current.ExportSettings.Folder)
    {
        InitializeComponent();
    }
}