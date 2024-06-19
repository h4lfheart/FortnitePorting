using System;
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
using FortnitePorting.Application;
using FortnitePorting.Export;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Sockets;
using FortnitePorting.Multiplayer;
using FortnitePorting.Services;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Multiplayer.Data;
using FortnitePorting.Shared.Models.Serilog;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ChatView : ViewBase<ChatViewModel>
{
    public ChatView()
    {
        InitializeComponent();
        ViewModel.Scroll = Scroll;
        TextBox.AddHandler(KeyDownEvent, Handler, RoutingStrategies.Tunnel);

        async void Handler(object? sender, KeyEventArgs e)
        {
            if (sender is not AutoCompleteBox autoCompleteBox) return;
            if (autoCompleteBox.GetVisualDescendants().FirstOrDefault(x => x is TextBox) is not TextBox textBox) return;
            if (textBox.Text is not { } text) return;
            if (string.IsNullOrWhiteSpace(text)) return;
            
            if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                if (text.StartsWith("/shrug"))
                {
                    await SocketService.Send(new MessageData(@"¯\_(ツ)_/¯"));
                }
                else
                {
                    await SocketService.Send(new MessageData(text));
                }
                textBox.Text = string.Empty;
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
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        Scroll.ScrollToEnd();
    }

    private async void OnYeahPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessage message) return;

        message.ReactedTo = !message.ReactedTo;
        await SocketService.Send(new ReactionData(message.Id, SocketService.User.ID, message.ReactedTo));
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
}