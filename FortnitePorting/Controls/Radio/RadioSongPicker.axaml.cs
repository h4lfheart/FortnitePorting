using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Sound;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.Services;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.Controls.Radio;

public partial class RadioSongPicker : UserControl
{
    public USoundCue SoundCue;
    public Bitmap CoverArtImage { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    
    public RadioSongPicker(UObject asset)
    {
        InitializeComponent();
        
        SoundCue = asset.Get<USoundCue>("FrontEndLobbyMusic");
        Title = asset.Get<FText>("DisplayName").Text;
        Description = asset.Get<FText>("Description").Text;

        var coverArtTexture = asset.Get<UTexture2D>("CoverArtImage");
        CoverArtImage = new Bitmap(coverArtTexture.Decode()!.Encode(SKEncodedImageFormat.Png, 100).AsStream());
    }

    private void OnSongPlayPressed(object? sender, PointerPressedEventArgs e)
    {
        RadioVM.Play(this);
    }
}