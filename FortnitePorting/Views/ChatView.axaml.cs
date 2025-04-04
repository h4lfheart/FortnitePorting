using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using Clowd.Clipboard;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models;
using FortnitePorting.Models.Chat;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ChatView : ViewBase<ChatViewModel>
{
    public ChatView() : base(ChatVM)
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
            if (text.Length > 400)
            {
                AppWM.Message("Character Limit", "Your message is over the character limit of 400 characters.");
                e.Handled = true;
                return;
            }
            
            if (text.StartsWith("/shrug"))
            {
                await OnlineService.Send(new MessagePacket(@"¯\_(ツ)_/¯"));
            }
            else if (ImageFlyout.IsOpen)
            {
                var memoryStream = new MemoryStream();
                ViewModel.SelectedImage.Save(memoryStream);
                await OnlineService.Send(new MessagePacket(text, memoryStream.ToArray(), ViewModel.SelectedImageName));
            }
            else
            {
                await OnlineService.Send(new MessagePacket(text));
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
        TextBox.Focus();
        AppWM.ChatNotifications = 0;
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
        await OnlineService.Send(new ReactionPacket(message.Id, message.ReactedTo));
    }

    private async void OnDeletePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessage message) return;
        
        await OnlineService.Send(new DeleteMessagePacket(), new MetadataBuilder().With("Id", message.Id));
    }

    private void OnMessageUserPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        
        FlyoutBase.ShowAttachedFlyout(control);
    }

    private async void OnReplyPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessage chatMessage) return;

        await chatMessage.User.SendMessage();
    }
}