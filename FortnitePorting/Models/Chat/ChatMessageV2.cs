using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Supabase.Tables;

namespace FortnitePorting.Models.Chat;

public partial class ChatMessageV2 : ObservableObject
{
    [ObservableProperty] private string _id;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TimestampString))] private DateTime _timestamp;
    [ObservableProperty] private ChatUserV2 _user;
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private string _application = string.Empty;
    [ObservableProperty] private bool _wasEdited;
    [ObservableProperty] private string? _replyId;
    [ObservableProperty] private string? _imagePath;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(DidReactTo), 
        nameof(ReactionBitmap),
        nameof(ReactionBrush)
    )]
    private string[] _reactorIds = [];

    
    
    [ObservableProperty] private bool _isEditing;

    [ObservableProperty] private ObservableDictionary<string, ChatMessageV2> _replyMessages = [];

    public string? FullImageUrl => ImagePath is not null
        ? $"https://supabase.fortniteporting.app/storage/v1/object/public/{ImagePath}"
        : null;


    public string TimestampString =>
        Timestamp.Date == DateTime.Today ? Timestamp.ToString("t") : Timestamp.ToString("g");

    public bool CanDelete => SupaBase.Permissions.Role >= ESupabaseRole.Staff || User!.UserId.Equals(SupaBase.UserInfo!.UserId);
    public bool CanEdit => User!.UserId.Equals(SupaBase.UserInfo!.UserId);
    public bool CanReply => ReplyId is null;
    
    
    public bool DidReactTo => ReactorIds.Contains(SupaBase.UserInfo!.UserId);
    public Bitmap ReactionBitmap => DidReactTo ? ReactOff : ReactOn;
    public SolidColorBrush ReactionBrush => DidReactTo ? SolidColorBrush.Parse("#FF6de400") : SolidColorBrush.Parse("#80FFFFFF");
    
    private static readonly Bitmap ReactOn = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/YeahOff.png");
    private static readonly Bitmap ReactOff = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/YeahOn.png");

    [RelayCommand]
    public async Task SaveImage()
    {
        if (await App.SaveFileDialog(ImagePath!.SubstringAfterLast("/")) is { } path)
        {
            await Api.DownloadFileAsync(FullImageUrl!, path);
        }
    }
}