using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using CUE4Parse.Utils;
using FortnitePorting.Framework;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Supabase.Tables;


using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.ViewModels;
using Serilog;
using Supabase.Storage.Exceptions;

namespace FortnitePorting.Views;

public partial class ChatView : ViewBase<ChatViewModel>
{
    private const double AutoScrollThreshold = 400;
    private const double LoadMoreThreshold = 200;
    
    private bool _shouldAutoScroll = true;
    private bool _didInitialScroll = false;
    private bool _isLoadingMore = false;
    
    public ChatView()
    {
        InitializeComponent();
        ViewModel.ImageFlyout = ImageFlyout;
        TextBox.AddHandler(KeyDownEvent, OnTextKeyDown, RoutingStrategies.Tunnel);

        Scroll.LayoutUpdated += (sender, args) =>
        {
            if (_didInitialScroll) return;
            
            Scroll.ScrollToEnd();
            _didInitialScroll = true;
        };

        Scroll.ScrollChanged += async (sender, args) =>
        {
            var distanceFromBottom = Scroll.Extent.Height - Scroll.Viewport.Height - Scroll.Offset.Y;
            _shouldAutoScroll = distanceFromBottom <= AutoScrollThreshold;

            if (_shouldAutoScroll)
            {
                ViewModel.ClearNewMessageIndicator();
            }

            // Check if we're near the top and should load more messages
            var distanceFromTop = Scroll.Offset.Y;
            if (distanceFromTop <= LoadMoreThreshold && !_isLoadingMore && Chat.HasMoreMessages)
            {
                await LoadMoreMessages();
            }
        };

        Chat.MessageReceived += (sender, args) =>
        {
            TaskService.RunDispatcher(() =>
            {
                var distanceFromBottom = Scroll.Extent.Height - Scroll.Viewport.Height - Scroll.Offset.Y;
                _shouldAutoScroll = distanceFromBottom <= AutoScrollThreshold;
                
                if (_shouldAutoScroll)
                {
                    Scroll.ScrollToEnd();
                }
                else
                {
                    ViewModel.IncrementNewMessageIndicator();
                }
            });
        };
    }

    private async Task LoadMoreMessages()
    {
        if (_isLoadingMore) return;
        
        _isLoadingMore = true;
        
        try
        {
            var previousExtentHeight = Scroll.Extent.Height;
            var previousOffset = Scroll.Offset.Y;

            var loaded = await Chat.LoadMoreMessages();

            if (loaded)
            {
                await Task.Delay(50);

                var newExtentHeight = Scroll.Extent.Height;
                var heightDifference = newExtentHeight - previousExtentHeight;
                var newOffset = previousOffset + heightDifference;

                Scroll.Offset = Scroll.Offset.WithY(newOffset);
            }
        }
        finally
        {
            _isLoadingMore = false;
        }
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

                    var fileNameWithoutExtension = ViewModel.SelectedImageName.SubstringBefore(".");
                    var extension = ViewModel.SelectedImageName.SubstringAfterLast(".");
                    var hash = memoryStream.GetHash();
                    var fileName = $"{fileNameWithoutExtension}.{hash}.{extension}";
                    
                    try
                    {
                        imagePath = await imageBucket.Upload(memoryStream.ToArray(), fileName);
                    }
                    catch (SupabaseStorageException e)
                    {
                        imagePath = $"chat-images/{fileName}";
                    }
                }
                
                await Chat.SendMessage(Chat.ConvertMentionsToIds(text), replyId: ViewModel.ReplyMessage?.Id, imagePath: imagePath);
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
        
        if (e.Key != Key.Enter)
        {
            var isTyping = !string.IsNullOrWhiteSpace(textBox.Text);
            TaskService.Run(async () =>
            {
                if (ViewModel.Chat.Presence.IsTyping != isTyping)
                {
                    ViewModel.Chat.Presence.IsTyping = isTyping;
                    await ViewModel.Chat.ChatPresence.Track(ViewModel.Chat.Presence);
                }
            });
        }
        
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        Scroll.ScrollToEnd();
        TextBox.Focus();
        ViewModel.ClearNewMessageIndicator();

        _didInitialScroll = false;
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
        if (control.DataContext is not ChatMessage message) return;

        TaskService.Run(async () => await Api.FortnitePorting.DeleteMessage(message.Id));
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

        ViewModel.ReplyMessage = chatMessage;
    }

    private void OnEditPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessage message) return;

        // TODO focus edit textbox
        message.IsEditing = !message.IsEditing;
    }

    private void OnEditBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        if (textBox.DataContext is not ChatMessage message) return;
        
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

    private void OnCopyPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        if (control.DataContext is not ChatMessage message) return;

        App.Clipboard.SetTextAsync(message.Text);
    }

    private void OnTextBoxTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not AutoCompleteBox autoCompleteBox) return;
        if (autoCompleteBox.GetVisualDescendants().FirstOrDefault(x => x is TextBox) is not TextBox textBox) return;
        if (textBox.Text is not { } text) return;
        
        if (string.IsNullOrWhiteSpace(text))
        {
            TaskService.Run(async () =>
            {
                ViewModel.Chat.Presence.IsTyping = false;
                await ViewModel.Chat.ChatPresence.Track(ViewModel.Chat.Presence);
            });
        }
    }

    private void OnNewMessageIndicatorPressed(object? sender, PointerPressedEventArgs e)
    {
        Scroll.ScrollToEnd();
        ViewModel.ClearNewMessageIndicator();
    }
}