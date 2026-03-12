using System;
using System.Collections.Generic;
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
    private const double AutoScrollThreshold = 1500;
    private const double LoadMoreThreshold = 200;
    private const int MentionPageSize = 8;

    private bool _shouldAutoScroll = true;
    private bool _didInitialScroll = false;
    private bool _isLoadingMore = false;
    private int _mentionStart = -1;

    public ChatView()
    {
        InitializeComponent();
        ViewModel.ImageFlyout = ImageFlyout;

        ViewModel.TextBox = TextBox;
        TextBox.AddHandler(KeyDownEvent, OnTextKeyDown, RoutingStrategies.Tunnel);
        TextBox.TextChanged += OnTextBoxTextChanged;
        MentionList.SelectionChanged += OnMentionSelectionChanged;

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
                ViewModel.ClearNewMessageIndicator();

            var distanceFromTop = Scroll.Offset.Y;
            if (distanceFromTop <= LoadMoreThreshold && !_isLoadingMore && Chat.HasMoreMessages)
                await LoadMoreMessages();
        };

        Chat.TypingUsers.CollectionChanged += (sender, args) =>
        {
            TaskService.RunDispatcher(() =>
            {
                if (_shouldAutoScroll)
                    Scroll.ScrollToEnd();
            });
        };

        Chat.MessageReceived += (sender, args) =>
        {
            TaskService.RunDispatcher(async () =>
            {
                await Task.Delay(50); // short delay to account for image load

                var distanceFromBottom = Scroll.Extent.Height - Scroll.Viewport.Height - Scroll.Offset.Y;
                _shouldAutoScroll = distanceFromBottom <= AutoScrollThreshold;

                if (_shouldAutoScroll)
                    Scroll.ScrollToEnd();
                else
                    ViewModel.IncrementNewMessageIndicator();
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
                var heightDifference = Scroll.Extent.Height - previousExtentHeight;
                Scroll.Offset = Scroll.Offset.WithY(previousOffset + heightDifference);
            }
        }
        finally
        {
            _isLoadingMore = false;
        }
    }

    private int FindMentionStart(string text, int caretIndex)
    {
        for (var i = caretIndex - 1; i >= 0; i--)
        {
            switch (text[i])
            {
                case '@':
                {
                    var isLeftValid = i == 0 || text[i - 1] == ' ' || text[i - 1] == '\n';
                    return isLeftValid ? i : -1;
                }
                case ' ':
                case '\n':
                    return -1;
            }
        }

        return -1;
    }

    private void UpdateMentionPopup()
    {
        var text = TextBox.Text ?? string.Empty;
        var caret = TextBox.CaretIndex;

        _mentionStart = FindMentionStart(text, caret);

        if (_mentionStart < 0)
        {
            CloseMentionPopup();
            return;
        }

        var query = text.Substring(_mentionStart + 1, caret - _mentionStart - 1).ToLowerInvariant();
        var isStaff = ViewModel.Chat.SupaBase.UserInfo?.Role >= ESupabaseRole.Staff;

        List<string> matchList;
        if (string.IsNullOrEmpty(query))
        {
            var onlineUsers = ViewModel.Chat.Users
                .Select(kvp => $"@{kvp.Value.UserName}")
                .Take(MentionPageSize)
                .ToList();

            matchList = isStaff
                ? onlineUsers.Append("@everyone").ToList()
                : onlineUsers;
        }
        else
        {
            matchList = ViewModel.Chat.UserMentionNames
                .Where(n => n.TrimStart('@').StartsWith(query, StringComparison.InvariantCultureIgnoreCase))
                .ToList();
        }

        if (matchList.Count == 0)
        {
            CloseMentionPopup();
            return;
        }

        MentionList.ItemsSource = matchList;
        MentionList.SelectedIndex = 0;
        MentionPopup.IsOpen = true;
    }

    private void CloseMentionPopup()
    {
        MentionPopup.IsOpen = false;
        _mentionStart = -1;
    }

    private void CommitMention(string mention)
    {
        if (_mentionStart < 0) return;

        var text = TextBox.Text ?? string.Empty;
        var caret = TextBox.CaretIndex;

        var before = text[.._mentionStart];
        var after = text[caret..];
        var inserted = mention + " ";

        TextBox.Text = before + inserted + after;
        TextBox.CaretIndex = before.Length + inserted.Length;

        CloseMentionPopup();
    }

    private void OnTextBoxTextChanged(object? sender, TextChangedEventArgs e)
    {
        var text = TextBox.Text ?? string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            TaskService.Run(async () =>
            {
                ViewModel.Chat.Presence.IsTyping = false;
                await ViewModel.Chat.ChatPresence.Track(ViewModel.Chat.Presence);
            });
        }

        UpdateMentionPopup();
    }

    public void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        var text = textBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) && !ImageFlyout.IsOpen) return;

        if (MentionPopup.IsOpen)
        {
            if (e.Key == Key.Down)
            {
                MentionList.SelectedIndex = Math.Min(MentionList.SelectedIndex + 1, MentionList.ItemCount - 1);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Up)
            {
                MentionList.SelectedIndex = Math.Max(MentionList.SelectedIndex - 1, 0);
                e.Handled = true;
                return;
            }

            if (e.Key is Key.Tab || (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift)))
            {
                if (MentionList.SelectedItem is string chosen)
                    CommitMention(chosen);
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                CloseMentionPopup();
                e.Handled = true;
                return;
            }
        }

        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (text.Length > 400)
            {
                Info.Message("Character Limit", "Your message is over the character limit of 400 characters.");
                e.Handled = true;
                return;
            }

            if (text.StartsWith("/shrug"))
                text = @"¯\_(ツ)_/¯";

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
                    catch (SupabaseStorageException)
                    {
                        imagePath = $"chat-images/{fileName}";
                    }
                }

                await Chat.SendMessage(Chat.ConvertMentionsToIds(text), replyId: ViewModel.ReplyMessage?.Id,
                    imagePath: imagePath);
                ViewModel.ReplyMessage = null;
            });

            textBox.Text = string.Empty;
            ImageFlyout.IsOpen = false;
            CloseMentionPopup();
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

    private void OnMentionListPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not ListBox lb) return;
        if (lb.SelectedItem is not string chosen) return;
        CommitMention(chosen);
        e.Handled = true;
        TextBox.Focus();
    }

    private void OnMentionSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        TextBox.Focus();
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
            await SupaBase.Client.Rpc("remove_reaction", new { message_id = message.Id });
        else
            await SupaBase.Client.Rpc("add_reaction", new { message_id = message.Id });
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
            TaskService.Run(async () => await Chat.UpdateMessage(message, newText));
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

    private void OnNewMessageIndicatorPressed(object? sender, PointerPressedEventArgs e)
    {
        Scroll.ScrollToEnd();
        ViewModel.ClearNewMessageIndicator();
    }
}