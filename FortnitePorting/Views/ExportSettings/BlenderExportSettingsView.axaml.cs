using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.ExportSettings;

namespace FortnitePorting.Views.ExportSettings;

public partial class BlenderExportSettingsView : ViewBase<BlenderExportSettingsViewModel>
{
    public BlenderExportSettingsView() : base(AppSettings.Current.ExportSettings.Blender)
    {
        InitializeComponent();
    }
}