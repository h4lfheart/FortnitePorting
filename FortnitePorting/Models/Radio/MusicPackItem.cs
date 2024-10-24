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
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
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
    [ObservableProperty] private UTexture2D? _alternateCoverTexture;
    [ObservableProperty] private string _id;
    [ObservableProperty] private string _filePath;
    [ObservableProperty] private string _trackName;
    [ObservableProperty] private string _trackDescription;
    [ObservableProperty] private string _coverArtName;
    [ObservableProperty] private FPackageIndex _soundWave;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(PlayIconKind))] private bool _isPlaying;
    public MaterialIconKind PlayIconKind => IsPlaying ? MaterialIconKind.Pause : MaterialIconKind.Play;
    
    public MusicPackItem(UObject asset)
    {
        Id = asset.Name;
        FilePath = asset.GetPathName();
        
        TrackName = asset.GetAnyOrDefault<FText?>("DisplayName", "ItemName")?.Text;
        TrackName ??= asset.Name;

        TrackDescription = asset.GetAnyOrDefault<FText?>("Description", "ItemDescription")?.Text;
        TrackDescription ??= "No description.";
        
        var coverArtImage = asset.Get<UTexture2D>("CoverArtImage");
        CoverArtName = coverArtImage.Name;
        CoverArtBitmap = coverArtImage.Decode()!.ToWriteableBitmap();

        AlternateCoverTexture = asset.GetAnyOrDefault<UTexture2D>("SmallPreviewImage", "LargePreviewImage", "Icon", "LargeIcon");
        AlternateCoverTexture ??= asset.GetDataListItem<UTexture2D>("SmallPreviewImage", "LargePreviewImage", "Icon", "LargeIcon");

        var lobbyMusic = asset.Get<UObject>("FrontEndLobbyMusic");
        if (lobbyMusic is USoundCue soundCue)
        {
            SoundWave = soundCue.HandleSoundTree().MaxBy(sound => sound.Time)?.SoundWave;
        }
        else if (lobbyMusic is UMetaSoundSource metaSoundSource) // TODO proper impl with class
        {
            var rootMetasoundDocument = metaSoundSource.GetOrDefault<FStructFallback?>("RootMetaSoundDocument") 
                                        ?? metaSoundSource.GetOrDefault<FStructFallback?>("RootMetasoundDocument");
            var rootGraph = rootMetasoundDocument.Get<FStructFallback>("RootGraph");
            var interFace = rootGraph.Get<FStructFallback>("Interface");
            var inputs = interFace.Get<FStructFallback[]>("Inputs");
            foreach (var input in inputs)
            {
                var typeName = input.Get<FName>("TypeName");
                if (!typeName.Text.Equals("WaveAsset")) continue;
                
                var name = input.Get<FName>("Name");
                if (!name.Text.Equals("Loop")) continue;

                var defaultLiteral = input.Get<FStructFallback>("DefaultLiteral");
                SoundWave = defaultLiteral.Get<FPackageIndex[]>("AsUObject").First();
                break;
            }
        }
    }
    
    public bool Match(string filter)
    {
        return MiscExtensions.Filter(TrackName, filter) || MiscExtensions.Filter(Id, filter);
    }


    [RelayCommand]
    public async Task SaveAudio()
    {
        var fileType = RadioVM.SoundFormat switch
        {
            ESoundFormat.MP3 => Globals.MP3FileType,
            ESoundFormat.WAV => Globals.WAVFileType,
            ESoundFormat.OGG => Globals.OGGFileType,
            ESoundFormat.FLAC => Globals.FLACFileType,
        };
        
        if (await SaveFileDialog(suggestedFileName: Id, fileType) is not { } path) return;
        await SaveAudio(path, RadioVM.SoundFormat);
    }

    public async Task SaveAudio(string path, ESoundFormat soundFormat)
    {
        await TaskService.RunAsync(async () =>
        {
            if (!SoundExtensions.TrySaveSoundToAssets(SoundWave.Load<USoundWave>(), AppSettings.Current.Application.AssetPath, out string wavPath)) return;

            if (File.Exists(path)) return;

            switch (soundFormat)
            {
                case ESoundFormat.WAV:
                {
                    File.Copy(wavPath, path);
                    
                    var track = new Track(path)
                    {
                        Title = TrackName,
                        Description = TrackDescription,
                        Artist = "Epic Games"
                    };
            
                    track.Save();
                    
                    break;
                }
                default:
                {
                    await FFMpegArguments.FromFileInput(wavPath)
                        .OutputToFile(path, true, options => options.ForceFormat(Path.GetExtension(path)[1..]))
                        .ProcessAsynchronously();
            
                    var file = new FileInfo(path);
                    ATL.Settings.ID3v2_writePictureDataLengthIndicator = false;
                    ATL.Settings.FileBufferSize = file.Length > int.MaxValue
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
                    break;
                }
            }
            
        });
    }
    
    public async Task SaveAudio(DirectoryInfo directory, ESoundFormat soundFormat)
    {
        var path = Path.Combine(directory.FullName, Id + ".mp3");
        await SaveAudio(path, soundFormat);
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

    [RelayCommand]
    public async Task CopyPath()
    {
        await Clipboard.SetTextAsync(FilePath);
    }

    private bool IsCustomPlaylist()
    {
        return !RadioVM.ActivePlaylist.IsDefault;
    }
}