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
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared.Models;
using FortnitePorting.Shared.Models.Clipboard;
using FortnitePorting.Shared.Services;
using Serilog;
using SixLabors.ImageSharp.PixelFormats;

namespace FortnitePorting.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty] private ScrollViewer? _scroll;
    [ObservableProperty] private TeachingTip _imageFlyout;

    [ObservableProperty] private EPermissions _permissions = EPermissions.None;
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private Bitmap _selectedImage;
    [ObservableProperty] private string _selectedImageName;

    [ObservableProperty] private ObservableCollection<ChatUser> _users = [];
    [ObservableProperty] private ObservableCollection<ChatMessage> _messages = [];
    [ObservableProperty] private ObservableCollection<string> _commands = [];
    [ObservableProperty] private bool _areServicesDown;
    
    public override async Task Initialize()
    {
        Messages.CollectionChanged += (sender, args) =>
        {
            TaskService.RunDispatcher(() =>
            {
                var isScrolledToEnd = Math.Abs(Scroll.Offset.Y - Scroll.Extent.Height + Scroll.Viewport.Height) < 500;
                if (isScrolledToEnd)
                    Scroll.ScrollToEnd();
            });
        };
    }

    [RelayCommand]
    public async Task OpenImage()
    {
        if (await BrowseFileDialog(fileTypes: Globals.ChatAttachmentFileType) is { } path)
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

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        switch (e.PropertyName)
        {
            case nameof(Messages) when Scroll is not null:
            {
                TaskService.RunDispatcher(Scroll.ScrollToEnd);
                break;
            }
        }
    }
}

