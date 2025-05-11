using System;
using System.Threading.Tasks;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FortnitePorting.Framework;
using FortnitePorting.Models.API.Responses;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Shared.Services;
using Mapster;

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
    
    [ObservableProperty] private bool _isEditing;

    [ObservableProperty] private ObservableDictionary<string, ChatMessageV2> _replyMessages = [];

    public string? FullImageUrl => ImagePath is not null
        ? $"https://backend.fortniteporting.halfheart.dev/storage/v1/object/public/{ImagePath}"
        : null;


    public string TimestampString =>
        Timestamp.Date == DateTime.Today ? Timestamp.ToString("t") : Timestamp.ToString("g");

    public bool CanDelete => SupaBase.Permissions.Role >= ESupabaseRole.Staff || User!.UserId.Equals(SupaBase.UserInfo!.UserId);
    public bool CanEdit => User!.UserId.Equals(SupaBase.UserInfo!.UserId);
    public bool CanReply => ReplyId is null;

    [RelayCommand]
    public async Task SaveImage()
    {
        if (await App.SaveFileDialog(ImagePath!.SubstringAfterLast("/")) is { } path)
        {
            await Api.DownloadFileAsync(FullImageUrl!, path);
        }
    }
}