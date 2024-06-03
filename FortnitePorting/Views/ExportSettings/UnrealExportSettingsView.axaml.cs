using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Application;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels.ExportSettings;

namespace FortnitePorting.Views.ExportSettings;

public partial class UnrealExportSettingsView : ViewBase<UnrealExportSettingsViewModel>
{
    public UnrealExportSettingsView() : base(AppSettings.Current.ExportSettings.Unreal)
    {
        InitializeComponent();
    }
}