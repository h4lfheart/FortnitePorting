using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ATL;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using CSCore.Codecs.WAV;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.Export;
using FortnitePorting.Extensions;
using FortnitePorting.Framework.Controls;
using FortnitePorting.Services;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.Controls.Radio;

public partial class RadioSongPicker : UserControl
{
    public readonly FPackageIndex SoundWave;
    public Bitmap CoverArtImage { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ID { get; set; }

    public RadioSongPicker(UObject asset)
    {
        InitializeComponent();

        ID = asset.Name;
        var titleText = asset.GetAnyOrDefault<FText?>("DisplayName", "ItemName") ?? new FText(asset.Name);
        Title = titleText.Text;
        var description = asset.GetAnyOrDefault<FText?>("Description", "ItemDescription") ?? new FText("No description.");
        Description = description.Text;

        var coverArtTexture = asset.Get<UTexture2D>("CoverArtImage");
        CoverArtImage = new Bitmap(coverArtTexture.Decode()!.Encode(SKEncodedImageFormat.Png, 100).AsStream());

        if (!asset.TryGetValue(out UObject lobbyMusic, "FrontEndLobbyMusic"))
        {
            return;
        }
        
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

                var literal = input.GetOrDefault<FStructFallback?>("DefaultLiteral");
                if (literal is null && input.TryGetValue(out FStructFallback[] defaults, "Defaults"))
                {
                    literal = defaults.FirstOrDefault()?.GetOrDefault<FStructFallback?>("Literal");
                }
                
                if (literal is null) continue;
                
                SoundWave = literal.Get<FPackageIndex[]>("AsUObject").First();
                
                break;
            }
        }
    }

    public USoundWave? GetSound()
    {
        if (SoundWave is null)
        {
            MessageWindow.Show("Unsupported Lobby Music Format", $"\"{Title}\" uses a new format for lobby music that is currently unsupported.");
            return null;
        }
        
        return SoundWave.Load<USoundWave>();

    }
    
    public async Task Save()
    {
        var sound = GetSound();
        if (sound is null) return;

        await ExportService.ExportAsync(sound, EAssetType.Sound, EExportTargetType.Folder);

        var exportPath = ExporterInstance.GetExportPath(sound, "wav");
        var track = new Track(exportPath)
        {
            Title = Title,
            Description = Description,
            Artist = "Epic Games"
        };
        track.Save();
    }

    private void OnSongPlayPressed(object? sender, PointerPressedEventArgs e)
    {
        RadioVM.Play(this);
    }
}