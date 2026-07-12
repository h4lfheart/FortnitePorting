using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Models.Supabase.Tables;
using FortnitePorting.Services;
using FortnitePorting.Views;

namespace FortnitePorting.Models.Chat;

public partial class ChatMessage : ObservableObject, IChatFeedItem
{
    [ObservableProperty] private string _id;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(TimestampString))] private DateTime _timestamp;
    [ObservableProperty] private ChatUser _user;
    [ObservableProperty] private string _text = string.Empty;
    [ObservableProperty] private string _application = string.Empty;
    [ObservableProperty] private bool _wasEdited;
    [ObservableProperty] private string? _replyId;
    [ObservableProperty] private string? _imagePath;
    
    [ObservableProperty] private string? _gameFilePath;
    [ObservableProperty] private Bitmap? _gameFileIcon;
    [ObservableProperty] private string? _gameFileDisplayName;
    [ObservableProperty] private bool _hasValidGameFile;

    public string? GameFileName => GameFilePath?.SubstringAfterLast("/").SubstringBefore(".");

    [ObservableProperty] private bool _isPing;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(DidReactTo), 
        nameof(ReactionBitmap),
        nameof(ReactionBrush),
        nameof(HasReactions),
        nameof(YeahMenuHeader)
    )]
    private string[] _reactorIds = [];
    

    [ObservableProperty] private ObservableCollection<ChatMessage> _replyMessages = [];

    public string? FullImageUrl => ImagePath is not null
        ? $"https://supabase.fortniteporting.app/storage/v1/object/public/{ImagePath}"
        : null;
    
    public string TimestampString =>
        Timestamp.Date == DateTime.Today ? Timestamp.ToString("t") : Timestamp.ToString("g");

    public bool CanDelete => SupaBase.Permissions.Role >= ESupabaseRole.Staff || User!.UserId.Equals(SupaBase.UserInfo!.UserId);
    public bool CanEdit => User!.UserId.Equals(SupaBase.UserInfo!.UserId);
    public bool CanReply => ReplyId is null;
    
    
    public bool DidReactTo => ReactorIds.Contains(SupaBase.UserInfo!.UserId);
    public bool HasReactions => ReactorIds.Length > 0;
    public string YeahMenuHeader => DidReactTo ? "Remove Yeah!" : "Yeah!";
    public Bitmap ReactionBitmap => DidReactTo ? ReactOff : ReactOn;
    public SolidColorBrush ReactionBrush => DidReactTo ? SolidColorBrush.Parse("#FF6de400") : SolidColorBrush.Parse("#80FFFFFF");
    
    private static readonly Bitmap ReactOn = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/YeahOff.png");
    private static readonly Bitmap ReactOff = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/YeahOn.png");

    [RelayCommand]
    public async Task React()
    {
        await Api.FortnitePorting.ReactToMessage(Id);
    }

    [RelayCommand]
    public async Task Delete()
    {
        await AppServices.Chat.DeleteMessage(this);
    }

    [RelayCommand]
    public void Reply()
    {
        ChatVM.ReplyMessage = this;
    }

    [RelayCommand]
    public void Edit()
    {
        ChatVM.EditMessage = this;
    }

    [RelayCommand]
    public void Copy()
    {
        App.Clipboard.SetTextAsync(Text);
    }

    [RelayCommand]
    public async Task SaveImage()
    {
        if (await App.SaveFileDialog(ImagePath!.SubstringAfterLast("/")) is { } path)
        {
            await Api.DownloadFileAsync(FullImageUrl!, path);
        }
    }

    [RelayCommand]
    public void NavigateToFiles()
    {
        if (string.IsNullOrEmpty(GameFilePath) || UEParse.Provider is null) return;
        Navigation.App.Open<FilesView>();
        FilesVM.JumpTo(UEParse.Provider.FixPath(GameFilePath));
        AppWM.Window.BringToTop();
    }

    public void LoadGameFileData()
    {
        if (string.IsNullOrEmpty(GameFilePath)) return;
        if (UEParse.Provider is null) return;
        if (GameFileIcon is not null) return;
        TaskService.Run(async () =>
        {
            var (icon, displayName, _) = await UEParse.ResolveGameFileAsync(GameFilePath);
            TaskService.RunDispatcher(() =>
            {
                GameFileIcon = icon;
                GameFileDisplayName = displayName;
                HasValidGameFile = true;
            });
        });
    }
}