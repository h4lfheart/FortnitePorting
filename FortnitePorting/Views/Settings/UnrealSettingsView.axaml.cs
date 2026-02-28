using System.Threading;
using Avalonia.Controls;
using FortnitePorting.Controls;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using FortnitePorting.Services;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class UnrealSettingsView : ViewBase<UnrealSettingsViewModel>
{
    private readonly EntranceTransition _transition = new();
    private CancellationTokenSource _cts = new();
    
    public UnrealSettingsView() : base(AppSettings.ExportSettings.Unreal)
    {
        InitializeComponent();
    }

    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        if (e.Tag is not Control control) return;
        
        SectionContent.Content = control;
        
        _cts.Cancel();
        _cts = new CancellationTokenSource();

        SectionContent.Content = control;
        TaskService.RunDispatcher(async () => await _transition.Start(null, SectionContent, true, _cts.Token));
    }
}