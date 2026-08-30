using System;
using System.Linq;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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

    private async void OnAddProfilePressed(object? sender, EventArgs e)
    {
        await ViewModel.AddProfile();
    }

    // spaces aint working so easy fix ??
    private void OnTextBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (e.Key != Key.Space) return;

        textBox.Text = textBox.Text!.Insert(textBox.CaretIndex, " ");
        textBox.CaretIndex++;
    }

    private void OnProfileContextMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (sender is not ContextMenu menu) return;

        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            if (item.Header?.ToString() == "Delete Profile")
                item.IsEnabled = ViewModel.CanRemoveProfiles;
        }
    }

    private async void OnDuplicateProfileClick(object? sender, RoutedEventArgs e)
    {
        if (GetProfile(sender) is not { } profile) return;
        await ViewModel.DuplicateProfile(profile);
    }

    private async void OnDeleteProfileClick(object? sender, RoutedEventArgs e)
    {
        if (GetProfile(sender) is not { } profile) return;
        await ViewModel.RemoveProfile(profile);
    }

    private static InstallationProfile? GetProfile(object? sender)
    {
        if (sender is not MenuItem item) return null;
        if (item.Tag is InstallationProfile tagged) return tagged;
        if (item.DataContext is InstallationProfile context) return context;
        if (item.Parent is ContextMenu { DataContext: InstallationProfile fromMenu }) return fromMenu;
        return null;
    }
}
