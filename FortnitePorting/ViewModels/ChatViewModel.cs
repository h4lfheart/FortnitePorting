using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Application;
using FortnitePorting.Models.Chat;
using FortnitePorting.Models.Sockets;
using FortnitePorting.Multiplayer.Data;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using Ionic.Zlib;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ZstdSharp;
using Color = Avalonia.Media.Color;
using Image = SixLabors.ImageSharp.Image;
using ImageExtensions = FortnitePorting.Shared.Extensions.ImageExtensions;

namespace FortnitePorting.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty] private ScrollViewer _scroll;
    [ObservableProperty] private ObservableCollection<ChatMessage> _messages = [];
    [ObservableProperty] private ObservableCollection<ChatUser> _users = [];
    [ObservableProperty] private string _text = string.Empty;

    [ObservableProperty] private ObservableCollection<string> _commands =
    [
        "/shrug"
    ];
    
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

    public override async void OnApplicationExit()
    {
        base.OnApplicationExit();
        await SocketService.Send(new UnregisterData());
    }

    private Compressor ZSTD_COMPRESS = new(6);

    [RelayCommand]
    public async Task SendImage()
    {
        if (await BrowseFileDialog(fileTypes: Globals.ImageFileType) is { } path)
        {
            var image = Image.Load(await File.ReadAllBytesAsync(path)).CloneAs<Rgba32>();

            var imageBytes = new byte[image.Width * image.Height * 4];
            image.CopyPixelDataTo(imageBytes);

            var compressedBytes = ZSTD_COMPRESS.Wrap(imageBytes).ToArray();
            var chunks = compressedBytes.Chunk(2048).ToArray();
            
            await SocketService.Send(new ImageHeaderData(Path.GetFileName(path), image.Width, image.Height, chunks.Length));
            for (var chunkIdx = 0; chunkIdx < chunks.Length; chunkIdx++)
            {
                await SocketService.Send(new ImageChunkData(chunkIdx, chunks[chunkIdx]));
            }
            await SocketService.Send(new ImageFooterData());
        }
    }

}

