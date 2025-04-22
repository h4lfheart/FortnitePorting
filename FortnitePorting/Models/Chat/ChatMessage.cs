using System;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Chat;

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty, NotifyPropertyChangedFor(nameof(UserTitleText))] private ChatUser _user;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasTextData))] private string _text;
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private DateTime _timestamp;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(HasImageData))] private Bitmap? _bitmap;
    [ObservableProperty] private string? _bitmapName;
    [ObservableProperty] private int _reactionCount;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(UserTitleText))] private bool _isPrivate;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(UserTitleText))] private string _targetUserName;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(BackgroundBrush))] private bool _isPing;

    public SolidColorBrush BackgroundBrush => new(IsPing ? Color.Parse("#0DFFFF62") : Color.Parse("#0DFFFFFF"));
    public SolidColorBrush TextBrush => new(IsPing ? Color.Parse("#10acff") : Colors.White);

    public string UserTitleText => IsPrivate ? $"Message {(User.DisplayName.Equals(AppSettings.Current.Online.GlobalName) ? $"To {TargetUserName}" : $"From {User.DisplayName}")}" : User.DisplayName;

    public bool HasImageData => Bitmap is not null;
    public bool HasTextData => !string.IsNullOrWhiteSpace(Text);
    
    
    [ObservableProperty, NotifyPropertyChangedFor(nameof(YeahImageSource))] private bool _reactedTo;
    public Bitmap YeahImageSource => ReactedTo ? YeahOn : YeahOff;

    private static Bitmap YeahOff = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/YeahOff.png");
    private static Bitmap YeahOn = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/YeahOn.png");
}