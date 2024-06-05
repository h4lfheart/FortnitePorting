using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ATL;
using ATL.AudioData;
using Avalonia.Media.Imaging;
using Commons;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using FFMpegCore;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Services;
using Material.Icons;
using SkiaSharp;

namespace FortnitePorting.Models.Radio;

public partial class MusicPackItem : ObservableObject
{
    [ObservableProperty] private WriteableBitmap _coverArtBitmap;
    [ObservableProperty] private UTexture2D _alternateCoverTexture;
    [ObservableProperty] private string _id;
    [ObservableProperty] private string _trackName;
    [ObservableProperty] private string _trackDescription;
    [ObservableProperty] private string _coverArtName;
    [ObservableProperty] private USoundCue _soundCue;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PlayIconKind))] private bool _isPlaying;
    public MaterialIconKind PlayIconKind => IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play;
    
    public MusicPackItem(UObject asset)
    {
        Id = asset.Name;
        
        TrackName = asset.GetAnyOrDefault<FText?>("DisplayName", "ItemName")?.Text;
        TrackName ??= asset.Name;

        TrackDescription = asset.GetAnyOrDefault<FText?>("Description", "ItemDescription")?.Text;
        TrackDescription ??= "No description.";
        
        var coverArtImage = asset.Get<UTexture2D>("CoverArtImage");
        CoverArtName = coverArtImage.Name;
        CoverArtBitmap = coverArtImage.Decode()!.ToWriteableBitmap();

        AlternateCoverTexture = asset.GetAnyOrDefault<UTexture2D>("LargePreviewImage", "SmallPreviewImage");

        SoundCue = asset.Get<USoundCue>("FrontEndLobbyMusic");
    }
    
    public bool Match(string filter)
    {
        return MiscExtensions.Filter(TrackName, filter) || MiscExtensions.Filter(Id, filter);
    }

    public USoundWave GetSound()
    {
        return SoundCue.HandleSoundTree().MaxBy(sound => sound.Time)?.SoundWave;
    }

    [RelayCommand]
    public async Task SaveAudio()
    {
        if (await SaveFileDialog(suggestedFileName: Id, Globals.MP3FileType) is not { } mp3Path) return;
        await SaveAudio(mp3Path);
    }

    public async Task SaveAudio(string path)
    {
        await TaskService.RunAsync(async () =>
        {
            var soundWave = GetSound();
            if (!SoundExtensions.TrySaveSoundToAssets(soundWave, AppSettings.Current.Application.AssetPath, out string wavPath)) return;

            if (File.Exists(path)) return;
            
            // convert to mp3
            await FFMpegArguments.FromFileInput(wavPath)
                .OutputToFile(path, true, options => options.ForceFormat("mp3"))
                .ProcessAsynchronously();
            
            var file = new FileInfo(path);
            Settings.ID3v2_writePictureDataLengthIndicator = false;
            Settings.FileBufferSize = file.Length > int.MaxValue
                ? int.MaxValue
                : (int) file.Length;
            
            // save metadata
            var coverStream = new MemoryStream();
            CoverArtBitmap.Save(coverStream);
            
            var track = new Track(path)
            {
                Title = TrackName,
                Description = TrackDescription,
                Artist = "Epic Games"
            };
            
            track.EmbeddedPictures.Add(PictureInfo.fromBinaryData(coverStream.ToArray(), PictureInfo.PIC_TYPE.Front));
            
            track.Save();
        });
    }
    
    public async Task SaveAudio(DirectoryInfo directory)
    {
        var path = Path.Combine(directory.FullName, Id + ".mp3");
        await SaveAudio(path);
    }
    
    [RelayCommand]
    public async Task SaveCoverArt()
    {
        await TaskService.RunAsync(async () =>
        {
            if (await SaveFileDialog(suggestedFileName: CoverArtName, Globals.PNGFileType) is not { } pngPath) return;
            CoverArtBitmap.Save(pngPath);
        });
    }
    
    [RelayCommand(CanExecute = nameof(IsCustomPlaylist))]
    public async Task RemoveFromPlaylist()
    {
        RadioVM.ActivePlaylist.MusicIDs.Remove(Id);
    }

    [RelayCommand(CanExecute = nameof(IsCustomPlaylist))]
    public async Task SetCoverForPlaylist()
    {
        RadioVM.ActivePlaylist.PlaylistCover = AlternateCoverTexture.Decode()!.ToWriteableBitmap();
        RadioVM.ActivePlaylist.PlaylistCoverPath = AlternateCoverTexture.GetPathName();
    }

    private bool IsCustomPlaylist()
    {
        return !RadioVM.ActivePlaylist.IsDefault;
    }
}