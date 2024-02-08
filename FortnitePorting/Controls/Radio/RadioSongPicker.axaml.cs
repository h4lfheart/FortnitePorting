using System.Collections.Generic;
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
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Export;
using FortnitePorting.Extensions;
using FortnitePorting.Services;
using SkiaSharp;

namespace FortnitePorting.Controls.Radio;

public partial class RadioSongPicker : UserControl
{
    public readonly USoundCue SoundCue;
    public Bitmap CoverArtImage { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string ID { get; set; }

    public RadioSongPicker(UObject asset)
    {
        InitializeComponent();

        ID = asset.Name;
        SoundCue = asset.Get<USoundCue>("FrontEndLobbyMusic");
        var titleText = asset.GetAnyOrDefault<FText?>("DisplayName", "ItemName") ?? new FText(asset.Name);
        Title = titleText.Text;
        var description = asset.GetAnyOrDefault<FText?>("Description", "ItemDescription") ?? new FText("No description.");
        Description = description.Text;

        var coverArtTexture = asset.Get<UTexture2D>("CoverArtImage");
        CoverArtImage = new Bitmap(coverArtTexture.Decode()!.Encode(SKEncodedImageFormat.Png, 100).AsStream());
    }

    public USoundWave? GetSound()
    {
        var sounds = SoundCue.HandleSoundTree();
        var sound = sounds.MaxBy(sound => sound.Time)?.SoundWave;
        return sound;

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