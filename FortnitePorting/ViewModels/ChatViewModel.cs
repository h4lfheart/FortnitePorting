using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Clipboard;
using FortnitePorting.Services;

namespace FortnitePorting.ViewModels;

public partial class ChatViewModel(SupabaseService supabase, ChatService chatService) : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase = supabase;
    [ObservableProperty] private ChatService _chat = chatService;

    [ObservableProperty] private ChatMessage? _replyMessage;
    
    [ObservableProperty] private TeachingTip _imageFlyout;
    
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private Bitmap _selectedImage;
    [ObservableProperty] private string _selectedImageName;

    public string MentionTextMatch => $"@{SupaBase.UserInfo.UserName}";
    
    [ObservableProperty] private bool _showNewMessageIndicator = false;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(NewMessageCountText))] private int _unreadMessageCount = 0;
    
    public string NewMessageCountText => UnreadMessageCount == 1 ? "1 New Message" : $"{UnreadMessageCount} New Messages";

    
    [RelayCommand]
    public async Task OpenImage()
    {
        if (await App.BrowseFileDialog(fileTypes: Globals.ChatAttachmentFileType) is { } path)
        {
            SelectedImageName = Path.GetFileName(path);
            SelectedImage = new Bitmap(path);
            ImageFlyout.IsOpen = true;
        }
    }

    public async Task ClipboardPaste()
    {
        if (await AvaloniaClipboard.GetTextAsync() is { } text)
        {
            Text += text;
        }
        else if (await AvaloniaClipboard.GetImageAsync() is { } image)
        {
            SelectedImageName = "clipboard.png";
            SelectedImage = image;
            ImageFlyout.IsOpen = true;
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
    }
}