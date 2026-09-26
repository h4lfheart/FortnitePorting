using System.Threading;
using Avalonia.Controls;
using FortnitePorting.Controls;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Services;

namespace FortnitePorting.Framework;

public abstract class SettingsSectionViewBase<T> : ViewBase<T> where T : ViewModelBase
{
    private readonly EntranceTransition _transition = new();
    private CancellationTokenSource _cts = new();

    protected SettingsSectionViewBase(T? templateViewModel = null) : base(templateViewModel)
    {
    }

    protected void ApplySidebarSection(ContentControl sectionContent, SidebarItemSelectedArgs e)
    {
        if (e.Tag is not Control control) return;

        sectionContent.Content = control;

        _cts.Cancel();
        _cts = new CancellationTokenSource();

        TaskService.RunDispatcher(async () => await _transition.Start(null, sectionContent, true, _cts.Token));
    }
}
