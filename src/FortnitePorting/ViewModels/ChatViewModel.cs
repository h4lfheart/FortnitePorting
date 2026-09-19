using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Framework;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Clipboard;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Services;
using FortnitePorting.Windows;

namespace FortnitePorting.ViewModels;

public partial class ChatViewModel(
    SupabaseService supabase,
    ChatService chat,
    FilesService files,
    APIService api,
    InfoService info,
    AppService app,
    DiscordService discord,
    CUE4ParseService ueParse) : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase = supabase;
    [ObservableProperty] private ChatService _chat = chat;
    [ObservableProperty] private FilesService _files = files;

    private readonly APIService _api = api;
    private readonly InfoService _info = info;
    private readonly AppService _app = app;
    private readonly DiscordService _discord = discord;
    private readonly CUE4ParseService _ueParse = ueParse;

    [ObservableProperty] private ChatMessage? _replyMessage;
    [ObservableProperty] private ChatMessage? _editMessage;
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private PendingImageAttachment? _pendingImage;
    [ObservableProperty] private PendingGameFileAttachment? _pendingGameFile;
    [ObservableProperty] private bool _showNewMessageIndicator;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(NewMessageCountText))] private int _unreadMessageCount;

    public string NewMessageCountText => UnreadMessageCount == 1 ? "1 New Message" : $"{UnreadMessageCount} New Messages";

    partial void OnEditMessageChanged(ChatMessage? value)
    {
        if (value is not null) ReplyMessage = null;
        Text = value?.Text ?? string.Empty;
    }

    partial void OnReplyMessageChanged(ChatMessage? value)
    {
        if (value is not null) EditMessage = null;
    }

    [RelayCommand]
    public void ClearEdit() => EditMessage = null;

    [RelayCommand]
    public async Task OpenImage()
    {
        if (await _app.BrowseFileDialog(fileTypes: Globals.ChatAttachmentFileType) is { } path)
            PendingImage = new PendingImageAttachment(new Bitmap(path), Path.GetFileName(path));
    }

    [RelayCommand]
    public void ClearImage() => PendingImage = null;

    [RelayCommand]
    public async Task OpenGameFile()
    {
        if (await FilePickerWindow.OpenBrowserAsync("Attach Game File") is { Length: > 0 } paths
            && paths.FirstOrDefault() is { } path)
        {
            var (icon, displayName, _) = await _ueParse.ResolveGameFileAsync(path);
            PendingGameFile = new PendingGameFileAttachment(path, icon, displayName);
        }
    }

    [RelayCommand]
    public void ClearGameFile() => PendingGameFile = null;

    [RelayCommand]
    public async Task ClipboardPaste()
    {
        if (await _app.Clipboard.GetTextAsync() is { } clipboardText)
        {
            Text += clipboardText;
        }
        else if (await AvaloniaClipboard.GetImageAsync() is { } image && _supaBase.UserInfo?.Role >= ESupabaseRole.Verified)
        {
            PendingImage = new PendingImageAttachment(image, "clipboard.png");
        }
    }

    public bool CanSubmit(string text)
        => !(string.IsNullOrWhiteSpace(text) && PendingImage is null && PendingGameFile is null && EditMessage is null);

    public bool ValidateLength(string text)
    {
        if (text.Length <= 400) return true;

        _info.Message("Character Limit", "Your message is over the character limit of 400 characters.");
        return false;
    }

    public async Task SendOrUpdateAsync(string text)
    {
        if (EditMessage is { } editMessage)
        {
            EditMessage = null;
            Text = string.Empty;
            await _chat.UpdateMessage(editMessage, text);
            return;
        }

        if (text.StartsWith("/shrug"))
            text = @"¯\_(?)_/¯";

        var pendingImage = PendingImage;
        var pendingGameFile = PendingGameFile;
        var replyId = ReplyMessage?.Id;

        string? imagePath = null;
        if (pendingImage is not null)
        {
            var memoryStream = new MemoryStream();
            pendingImage.Bitmap.Save(memoryStream);

            var result = await _api.FortnitePorting.UploadImage(memoryStream.ToArray(), pendingImage.Name);
            imagePath = result?.Path;
        }

        await _chat.SendMessage(_chat.ConvertMentionsToIds(text), replyId: replyId,
            imagePath: imagePath, gameFilePath: pendingGameFile?.Path);

        ReplyMessage = null;
        Text = string.Empty;
        ClearImage();
        ClearGameFile();
    }

    public async Task UpdateTypingAsync(bool isTyping)
    {
        if (_chat.Presence.IsTyping == isTyping) return;

        _chat.Presence.IsTyping = isTyping;
        await _chat.ChatPresence.Track(_chat.Presence);
    }

    public async Task<bool> LoadMoreMessagesAsync() => await _chat.LoadMoreMessages();

    public void IncrementNewMessageIndicator()
    {
        UnreadMessageCount++;
        ShowNewMessageIndicator = true;
    }

    public void ClearNewMessageIndicator()
    {
        UnreadMessageCount = 0;
        ShowNewMessageIndicator = false;
    }

    public override async Task OnViewOpened()
    {
        _discord.Update($"Chatting with {_chat.Users.Count} {(_chat.Users.Count > 1 ? "Users" : "User")}");

        _chat.UnseenMessageCount = 0;

        if (!_chat.HasFetchedMessages)
            await _chat.LoadMoreMessages();
    }
}