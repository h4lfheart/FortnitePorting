using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using FortnitePorting.Framework;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Services;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ChatView : ViewBase<ChatViewModel>
{
    private const double AutoScrollThreshold = 1500;
    private const double LoadMoreThreshold = 200;
    private const int MentionPageSize = 8;

    private bool _shouldAutoScroll = true;
    private bool _didInitialScroll = false;
    private bool _isLoadingMore = false;
    private double _lastExtentHeight = 0;
    private int _mentionStart = -1;

    public ChatView()
    {
        InitializeComponent();

        TextBox.AddHandler(KeyDownEvent, OnTextKeyDown, RoutingStrategies.Tunnel);
        TextBox.TextChanged += OnTextBoxTextChanged;
        MentionList.SelectionChanged += OnMentionSelectionChanged;

        Scroll.LayoutUpdated += (_, _) =>
        {
            var currentExtent = Scroll.Extent.Height;

            if (!_didInitialScroll)
            {
                if (currentExtent <= Scroll.Viewport.Height) return;
                _didInitialScroll = true;
            }

            var extentGrew = currentExtent > _lastExtentHeight;
            _lastExtentHeight = currentExtent;

            if (_shouldAutoScroll && extentGrew)
                Scroll.ScrollToEnd();
        };

        Scroll.ScrollChanged += async (_, _) =>
        {
            var distanceFromBottom = Scroll.Extent.Height - Scroll.Viewport.Height - Scroll.Offset.Y;
            _shouldAutoScroll = distanceFromBottom <= AutoScrollThreshold;

            if (_shouldAutoScroll)
                ViewModel.ClearNewMessageIndicator();

            var distanceFromTop = Scroll.Offset.Y;
            if (_didInitialScroll && distanceFromTop <= LoadMoreThreshold && !_isLoadingMore && ViewModel.Chat.HasMoreMessages)
                await LoadMoreMessages();
        };

        ViewModel.Chat.TypingUsers.CollectionChanged += (_, _) =>
        {
            TaskService.RunDispatcher(() =>
            {
                if (_shouldAutoScroll)
                    Scroll.ScrollToEnd();
            });
        };

        ViewModel.Chat.MessageReceived += (_, _) =>
        {
            TaskService.RunDispatcher(async () =>
            {
                await Task.Delay(50);

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

            var loaded = await ViewModel.LoadMoreMessagesAsync();
            if (!loaded) return;

            var tcs = new TaskCompletionSource();
            EventHandler? onLayout = null;
            onLayout = (_, _) =>
            {
                if (Scroll.Extent.Height <= previousExtentHeight) return;
                Scroll.LayoutUpdated -= onLayout;
                tcs.TrySetResult();
            };
            Scroll.LayoutUpdated += onLayout;
            await Task.WhenAny(tcs.Task, Task.Delay(1000));
            Scroll.LayoutUpdated -= onLayout;

            var heightDifference = Scroll.Extent.Height - previousExtentHeight;
            if (heightDifference > 0)
                Scroll.Offset = Scroll.Offset.WithY(previousOffset + heightDifference);
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
        var isStaff = ViewModel.SupaBase.UserInfo?.Role >= ESupabaseRole.Staff;

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
        ViewModel.Text = text;

        if (string.IsNullOrWhiteSpace(text))
            TaskService.Run(async () => await ViewModel.UpdateTypingAsync(false));

        UpdateMentionPopup();
    }

    public void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        var text = textBox.Text ?? string.Empty;

        if (e.Key == Key.Escape && ViewModel.EditMessage is not null)
        {
            ViewModel.EditMessage = null;
            e.Handled = true;
            return;
        }

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

        if (!ViewModel.CanSubmit(text)) return;

        if (e.Key == Key.Enter && !e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            if (!ViewModel.ValidateLength(text))
            {
                e.Handled = true;
                return;
            }

            var sendText = text;
            textBox.Text = string.Empty;
            CloseMentionPopup();
            Scroll.ScrollToEnd();
            TaskService.Run(async () => await ViewModel.SendOrUpdateAsync(sendText));
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
            TaskService.Run(async () => await ViewModel.UpdateTypingAsync(isTyping));
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
        _lastExtentHeight = 0;
    }

    private void OnUserPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        FlyoutBase.ShowAttachedFlyout(control);
    }

    private void OnMessageActionsClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        FlyoutBase.ShowAttachedFlyout(button);
    }

    private void OnMessageUserPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control) return;
        FlyoutBase.ShowAttachedFlyout(control);
    }

    private void OnEditCancelled(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.EditMessage = null;
    }

    private void OnReplyCancelled(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.ReplyMessage = null;
    }

    private void OnImageCancelled(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.ClearImage();
    }

    private void OnGameFileCancelled(object? sender, PointerPressedEventArgs e)
    {
        ViewModel.ClearGameFile();
    }

    private void OnNewMessageIndicatorPressed(object? sender, PointerPressedEventArgs e)
    {
        Scroll.ScrollToEnd();
        ViewModel.ClearNewMessageIndicator();
    }
}
