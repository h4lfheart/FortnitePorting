using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
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

public partial class ChatViewModel(SupabaseService supabase, ChatService chatService, FilesService filesService) : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase = supabase;
    [ObservableProperty] private ChatService _chat = chatService;
    [ObservableProperty] private FilesService _files = filesService;

    [ObservableProperty] private ChatMessage? _replyMessage;
    
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private TextBox _textBox;

    [ObservableProperty] private PendingImageAttachment? _pendingImage;
    [ObservableProperty] private PendingGameFileAttachment? _pendingGameFile;
    
    [ObservableProperty] private bool _showNewMessageIndicator = false;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(NewMessageCountText))] private int _unreadMessageCount = 0;
    
    public string NewMessageCountText => UnreadMessageCount == 1 ? "1 New Message" : $"{UnreadMessageCount} New Messages";

    
    [RelayCommand]
    public async Task OpenImage()
    {
        if (await App.BrowseFileDialog(fileTypes: Globals.ChatAttachmentFileType) is { } path)
            PendingImage = new PendingImageAttachment(new Bitmap(path), Path.GetFileName(path));
    }

    [RelayCommand]
    public void ClearImage()
    {
        PendingImage = null;
    }

    [RelayCommand]
    public async Task OpenGameFile()
    {
        if (await FilePickerWindow.OpenBrowserAsync("Attach Game File") is { Length: > 0 } paths
            && paths.FirstOrDefault() is { } path)
        {
            var (icon, displayName, _) = await UEParse.ResolveGameFileAsync(path);
            PendingGameFile = new PendingGameFileAttachment(path, icon, displayName);
        }
    }

    [RelayCommand]
    public void ClearGameFile()
    {
        PendingGameFile = null;
    }

    public async Task ClipboardPaste()
    {
        if (await App.Clipboard.GetTextAsync() is { } text)
        {
            var caret = TextBox.CaretIndex;
            var current = TextBox.Text ?? string.Empty;
            TextBox.Text = current.Insert(caret, text);
            TextBox.CaretIndex = caret + text.Length;
        }
        else if (await AvaloniaClipboard.GetImageAsync() is { } image && SupaBase.UserInfo?.Role >= ESupabaseRole.Verified)
        {
            PendingImage = new PendingImageAttachment(image, "clipboard.png");
        }
    }
    
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
        Discord.Update($"Chatting with {Chat.Users.Count} {(Chat.Users.Count > 1 ? "Users" : "User")}");

        Chat.UnseenMessageCount = 0;

        if (!Chat.HasFetchedMessages)
            await Chat.LoadMoreMessages();
    }

}