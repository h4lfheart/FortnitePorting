using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.Extensions;
using FortnitePorting.Shared.Extensions;
using Newtonsoft.Json;

namespace FortnitePorting.Models.Radio;

public partial class RadioPlaylist : ObservableObject
{
    [ObservableProperty] private bool _isDefault;
    [ObservableProperty] private Bitmap _playlistCover;
    [ObservableProperty] private string _playlistName;
    [ObservableProperty] private string _playlistCoverPath;
    [ObservableProperty] private ObservableCollection<string> _musicIDs = [];

    [JsonIgnore] public static RadioPlaylist Default = new(true);

    public RadioPlaylist(bool isDefault)
    {
        IsDefault = isDefault;
        PlaylistCover = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/FN/DefaultPlaylistImage.png");
        PlaylistName = isDefault ? "Default Playlist" : "New Playlist";
    }
    
    public RadioPlaylist()
    {
        
    }

    public static async Task<RadioPlaylist> FromSerializeData(RadioPlaylistSerializeData serializeData)
    {
        var playlist = new RadioPlaylist();
        playlist.IsDefault = false;
        playlist.PlaylistName = serializeData.Name;
        playlist.MusicIDs = new ObservableCollection<string>(serializeData.MusicIDs);
        if (serializeData.CoverArtPath is not null && await CUE4ParseVM.Provider.SafeLoadPackageObjectAsync<UTexture2D>(serializeData.CoverArtPath) is { } coverArt)
        {
            playlist.PlaylistCoverPath = serializeData.CoverArtPath;
            playlist.PlaylistCover = coverArt.Decode()!.ToWriteableBitmap();
        }
        else
        {
            playlist.PlaylistCover = ImageExtensions.AvaresBitmap("avares://FortnitePorting/Assets/FN/DefaultPlaylistImage.png");
        }

        return playlist;
    }

    [RelayCommand]
    public void AddToPlaylist(string id)
    {
        MusicIDs.Add(id);
    }

    public bool ContainsID(string id)
    {
        return IsDefault || MusicIDs.Contains(id);
    }
}

public class RadioPlaylistSerializeData
{
    public string Name;
    public string? CoverArtPath;
    public string[] MusicIDs;

    public static RadioPlaylistSerializeData FromPlaylist(RadioPlaylist playlist)
    {
        return new RadioPlaylistSerializeData
        {
            Name = playlist.PlaylistName,
            CoverArtPath = playlist.PlaylistCoverPath,
            MusicIDs = playlist.MusicIDs.ToArray()
        };
    }
}