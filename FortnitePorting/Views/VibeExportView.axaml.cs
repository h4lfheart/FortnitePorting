using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FortnitePorting.Framework;
using FortnitePorting.Models.Information;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class VibeExportView : ViewBase<VibeExportViewModel>
{
    public VibeExportView()
    {
        InitializeComponent();
        ViewModel.TextBox = NormalTextBox;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        WelcomeTextBox.Focus();
    }

    private void OnWelcomeKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (e.Key != Key.Enter) return;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            textBox.Text += "\n";
            textBox.CaretIndex = textBox.Text.Length;
            return;
        }
        
        if (string.IsNullOrWhiteSpace(ViewModel.PromptText)) return;

        ViewModel.IsWelcomeVisible = false;
        TaskService.Run(ViewModel.SendPrompt);
        NormalTextBox.Focus();
    }

    private void OnNormalChatKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (e.Key != Key.Enter) return;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            textBox.Text += "\n";
            textBox.CaretIndex = textBox.Text.Length;
            return;
        }
        
        if (string.IsNullOrWhiteSpace(ViewModel.PromptText)) return;
        
        TaskService.Run(ViewModel.SendPrompt);
        NormalTextBox.Focus();
        MessageScroll.ScrollToEnd();
    }
}