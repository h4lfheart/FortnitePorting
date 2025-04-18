using Avalonia.Controls;
using Avalonia.Input;
using FortnitePorting.Application;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels.Settings;
using Serilog;

namespace FortnitePorting.Views.Settings;

public partial class InstallationSettingsView : ViewBase<InstallationSettingsViewModel>
{
    public InstallationSettingsView() : base(AppSettings.Current.Installation)
    {
        InitializeComponent();
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