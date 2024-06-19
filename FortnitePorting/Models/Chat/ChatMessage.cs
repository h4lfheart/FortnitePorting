using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Chat;

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty] private ChatUser _user;
    [ObservableProperty] private string _text;
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private DateTime _timestamp;
    [ObservableProperty] private int _reactionCount;
    [ObservableProperty] private string _bitmapName;
    [ObservableProperty] private Bitmap _bitmap;
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(Source))] private bool _reactedTo;
    public Bitmap Source => ReactedTo ? On : Off;

    private static Bitmap Off = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/YeahOff.png");
    private static Bitmap On = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/YeahOn.png");
}