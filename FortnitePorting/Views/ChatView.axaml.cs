using System;
using System.IO;
using System.Linq;
using AsyncImageLoader;
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
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared.Services;
using FortnitePorting.ViewModels;
using Supabase.Storage.Exceptions;

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

    public void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not AutoCompleteBox autoCompleteBox) return;
        if (autoCompleteBox.GetVisualDescendants().FirstOrDefault(x => x is TextBox) is not TextBox textBox) return;
        if (textBox.Text is not { } text) return;
        if (string.IsNullOrWhiteSpace(text) && !ImageFlyout.IsOpen) return;
        
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (text.Length > 400)
            {
                Info.Message("Character Limit", "Your message is over the character limit of 400 characters.");
                e.Handled = true;
                return;
            }
            
            if (text.StartsWith("/shrug"))
            {
                text = @"¯\_(ツ)_/¯";
            }

            var shouldUploadImage = ImageFlyout.IsOpen;
            TaskService.Run(async () =>
            {
                string? imagePath = null;
                if (shouldUploadImage)
                {
                    var imageBucket = SupaBase.Client.Storage.From("chat-images");
                    var memoryStream = new MemoryStream();
                    ViewModel.SelectedImage.Save(memoryStream);

                    try
                    {
                        imagePath = await imageBucket.Upload(memoryStream.ToArray(), ViewModel.SelectedImageName);
                    }
                    catch (SupabaseStorageException e)
                    {
                        imagePath = $"chat-images/{ViewModel.SelectedImageName}";
                    }
                }
                
                await Chat.SendMessage(text, replyId: ViewModel.ReplyMessage?.Id, imagePath: imagePath);
                ViewModel.ReplyMessage = null;
            });
            
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
        //AppWM.ChatNotifications = 0;
    }

    private void OnUserPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        
        FlyoutBase.ShowAttachedFlyout(control);
    }

    private async void OnYeahPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessageV2 message) return;

        if (message.DidReactTo)
        {
            await SupaBase.Client.Rpc("remove_reaction", new { message_id = message.Id });
        }
        else
        {
            await SupaBase.Client.Rpc("add_reaction", new { message_id = message.Id });
        }
    }

    private void OnDeletePressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessageV2 message) return;

        TaskService.Run(async () =>
        {
            await SupaBase.Client.From<Message>()
                .Where(x => x.Id == message.Id)
                .Delete();
        });
    }

    private void OnMessageUserPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        
        FlyoutBase.ShowAttachedFlyout(control);
    }

    private async void OnReplyPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessageV2 chatMessage) return;

        ViewModel.ReplyMessage = chatMessage;
    }

    private void OnEditPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessageV2 message) return;

        // TODO focus edit textbox
        message.IsEditing = !message.IsEditing;
    }

    private void OnEditBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (textBox.DataContext is not ChatMessageV2 message) return;
        
        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            message.IsEditing = false;

            var newText = textBox.Text!;
            TaskService.Run(async () =>
            {
                await Chat.UpdateMessage(message, newText);
            });
        }
        else if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            textBox.Text += "\n";
            textBox.CaretIndex = textBox.Text.Length;
        }
    }

    private void OnReplyCancelled(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.ReplyMessage = null;
    }
}