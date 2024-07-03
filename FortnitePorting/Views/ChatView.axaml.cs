using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.Chat;
using FortnitePorting.Multiplayer.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ChatView : ViewBase<ChatViewModel>
{
    public ChatView()
    {
        InitializeComponent();
        ViewModel.Scroll = Scroll;
        ViewModel.ImageFlyout = ImageFlyout;
        TextBox.AddHandler(KeyDownEvent, OnTextKeyDown, RoutingStrategies.Tunnel);
    }

    public async void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not AutoCompleteBox autoCompleteBox) return;
        if (autoCompleteBox.GetVisualDescendants().FirstOrDefault(x => x is TextBox) is not TextBox textBox) return;
        if (textBox.Text is not { } text) return;
        if (string.IsNullOrWhiteSpace(text) && !ImageFlyout.IsOpen) return;
        
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (text.StartsWith("/shrug"))
            {
                await GlobalChatService.Send(new MessagePacket(@"¯\_(ツ)_/¯"));
            }
            else if (ImageFlyout.IsOpen)
            {
                await GlobalChatService.Send(new MessagePacket(text, await File.ReadAllBytesAsync(ViewModel.SelectedImagePath), ViewModel.SelectedImageName));
            }
            else
            {
                await GlobalChatService.Send(new MessagePacket(text));
            }
            
            textBox.Text = string.Empty;
            ImageFlyout.IsOpen = false;
            Scroll.ScrollToEnd();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            textBox.Text += "\n";
            textBox.CaretIndex = textBox.Text.Length;
            e.Handled = true;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        Scroll.ScrollToEnd();
    }
    
    private async void OnImagePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessage message) return;

        var dialog = new ContentDialog
        {
            Title = message.BitmapName,
            Content = new Border
            {
                CornerRadius = new CornerRadius(4),
                ClipToBounds = true,
                Child = new Image
                {
                    Source = message.Bitmap
                }
            },
            CloseButtonText = "Close",
            PrimaryButtonText = "Save",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                if (await SaveFileDialog(message.BitmapName, fileTypes: FilePickerFileTypes.ImagePng) is { } path)
                {
                    message.Bitmap.Save(path);
                }
            })
        };

        await dialog.ShowAsync();
    }

    private void OnUserPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        
        FlyoutBase.ShowAttachedFlyout(control);
    }

    private async void OnYeahPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessage message) return;

        message.ReactedTo = !message.ReactedTo;
        await GlobalChatService.Send(new ReactionPacket(message.Id, message.ReactedTo));
    }
}