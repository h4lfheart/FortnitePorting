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
using FortnitePorting.Models.Sockets;
using FortnitePorting.Multiplayer.Data;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Framework;
using FortnitePorting.Shared.Services;
using Ionic.Zlib;
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
    [ObservableProperty] private ObservableCollection<ChatEmoji> _emojis = [];
    [ObservableProperty] private string _text = string.Empty;

    [ObservableProperty] private ObservableCollection<string> _commands =
    [
        "/export",
        "/message",
        "/shrug"
    ];

    public ChatViewModel()
    {
        var fields = typeof(Emoji)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(fi => fi is { IsLiteral: true, IsInitOnly: false })
            .ToList();

        foreach (var field in fields)
        {
            if (field.Name.Equals("SmilingFace")) continue;
            Emojis.Add(new ChatEmoji
            {
                Name = field.Name,
                Character = (string) field.GetRawConstantValue()!
            });
        }
    }
    
    public override async Task Initialize()
    {
        SocketService.Send(new RegisterData());
        
        Messages.CollectionChanged += (sender, args) =>
        {
            TaskService.RunDispatcher(() =>
            {
                var isScrolledToEnd = Math.Abs(Scroll.Offset.Y - Scroll.Extent.Height + Scroll.Viewport.Height) < 250;
                if (isScrolledToEnd)
                    Scroll.ScrollToEnd();
            });
        };
    }

    public override void OnApplicationExit()
    {
        base.OnApplicationExit();
        SocketService.Send(new UnregisterData());
    }

}

public partial class ChatUser : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Brush))] private string _name;
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string _avatar;
    
    public SolidColorBrush Brush => Name switch
    {
        "SYSTEM" => new SolidColorBrush(Color.Parse("#B040FF")),
        "EXPORT" => new SolidColorBrush(Color.Parse("#39fbdc")),
        "Half" => new SolidColorBrush(Color.Parse("#10acff")),
        _ => new SolidColorBrush(Colors.White)
    };
}

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty] private ChatUser _user;
    [ObservableProperty] private string _text;
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private DateTime _timestamp;
    [ObservableProperty] private int _reactionCount;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Source))] private bool _reactedTo;
    public Bitmap Source => ReactedTo ? On : Off;

    private static Bitmap Off = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/YeahOff.png");
    private static Bitmap On = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/YeahOn.png");
}

public partial class ChatEmoji : ObservableObject
{
    [ObservableProperty] private string _name;
    [ObservableProperty] private string _character;

    [RelayCommand]
    public async Task Paste()
    {
        ChatVM.Text += Character;
    }
}
