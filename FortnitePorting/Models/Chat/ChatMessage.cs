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
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FortnitePorting.Application;
using FortnitePorting.Exporting;
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
        nameof(ReactionBrush)
    )]
    private string[] _reactorIds = [];
    
    [ObservableProperty] private bool _isEditing;

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
        TaskService.Run(() =>
        {
            Bitmap? icon = null;
            string? displayName = null;
            var fileName = GameFileName;

            if (!UEParse.Provider.TryLoadPackage(UEParse.Provider!.FixPath(GameFilePath), out var package))
                return;

            for (var i = 0; i < package.ExportMapLength; i++)
            {
                var pointer = new FPackageIndex(package, i + 1).ResolvedObject;
                if (pointer?.Object is null) continue;
                if (!pointer.Name.Text.Equals(fileName) &&
                    !pointer.Name.Text.Equals(fileName + "_C")) continue;

                var obj = ((AbstractUePackage) package).ConstructObject(pointer.Class, package);

                if (obj is UTexture2D && pointer.TryLoad(out var textureObj) &&
                    textureObj is UTexture2D texture &&
                    texture.Decode(maxMipSize: 128) is { } decodedTexture)
                {
                    icon = decodedTexture.ToWriteableBitmap();
                    break;
                }

                var assetLoader = AssetLoading.Categories
                    .SelectMany(category => category.Loaders)
                    .FirstOrDefault(loader => loader.ClassNames.Contains(obj.ExportType));
                if (assetLoader is not null && pointer.TryLoad(out var assetObj))
                {
                    icon = (assetLoader.LowResIconHandler(assetObj) ?? assetLoader.HighResIconHandler(assetObj))
                        ?.Decode(maxMipSize: 128)?.ToWriteableBitmap();
                    displayName = assetLoader.DisplayNameHandler(assetObj);
                    break;
                }

                displayName = obj.GetAnyOrDefault<FText?>("DisplayName", "ItemName")?.Text;

                if (obj.GetEditorIconBitmap() is { } editorIcon)
                {
                    icon = editorIcon;
                    break;
                }

                if (Exporter.DetermineExportType(obj) is var exportType and not EExportType.None
                    && $"avares://FortnitePorting/Assets/FN/{exportType}.png" is { } exportIconPath
                    && AssetLoader.Exists(new Uri(exportIconPath)))
                {
                    icon = ImageExtensions.AvaresBitmap(exportIconPath);
                    break;
                }
            }

            icon ??= ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/Unreal/DataAsset_64x.png");
            displayName ??= fileName;

            TaskService.RunDispatcher(() =>
            {
                GameFileIcon = icon;
                GameFileDisplayName = displayName;
                HasValidGameFile = true;
            });
        });
    }
}