using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using FortnitePorting.Controls;
using FortnitePorting.Controls.Navigation.Sidebar;
using FortnitePorting.Framework;
using FortnitePorting.Models.Installation;
using FortnitePorting.Services;
using FortnitePorting.ViewModels.Settings;

namespace FortnitePorting.Views.Settings;

public partial class InstallationSettingsView : ViewBase<InstallationSettingsViewModel>
{
    private readonly EntranceTransition _transition = new();
    private CancellationTokenSource _cts = new();

    public InstallationSettingsView() : base(AppSettings.Installation)
    {
        InitializeComponent();
    }

    private void OnItemSelected(object? sender, SidebarItemSelectedArgs e)
    {
        if (e.Tag is not InstallationProfile) return;
        if (!AppSettings.Application.UseTabTransitions) return;

        _cts.Cancel();
        _cts = new CancellationTokenSource();

        TaskService.RunDispatcher(async () => await _transition.Start(null, ProfileContent, true, _cts.Token));
    }

    // spaces aint working so easy fix ??
    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (e.Key != Key.Space) return;

        textBox.Text = textBox.Text!.Insert(textBox.CaretIndex, " ");
        textBox.CaretIndex++;
    }
}
