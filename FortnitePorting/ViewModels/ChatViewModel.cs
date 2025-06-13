using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media.Imaging;
using Clowd.Clipboard;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse;
using DynamicData;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Framework;
using FortnitePorting.Models;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Clipboard;
using FortnitePorting.Services;
using Serilog;
using SixLabors.ImageSharp.PixelFormats;
using Globals = FortnitePorting.Globals;

namespace FortnitePorting.ViewModels;

public partial class ChatViewModel(SupabaseService supabase, ChatService chatService) : ViewModelBase
{
    [ObservableProperty] private SupabaseService _supaBase = supabase;
    [ObservableProperty] private ChatService _chat = chatService;

    [ObservableProperty] private ChatMessageV2? _replyMessage;
    
    [ObservableProperty] private TeachingTip _imageFlyout;
    
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private Bitmap _selectedImage;
    [ObservableProperty] private string _selectedImageName;

    // TODO do we need this anymore
    [ObservableProperty] private ObservableCollection<string> _commands = [];
    

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
}

