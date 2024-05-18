using System;
using System.ComponentModel;
using System.Numerics;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Sounds;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.Extensions;
using FortnitePorting.Framework;
using FortnitePorting.Framework.ViewModels;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;

namespace FortnitePorting.ViewModels;

public partial class TexturePreviewViewModel : ViewModelBase
{
    [ObservableProperty] private bool useRedChannel = true;
    [ObservableProperty] private bool useGreenChannel = true;
    [ObservableProperty] private bool useBlueChannel = true;
    [ObservableProperty] private bool useAlphaChannel = true;
    
    [ObservableProperty] private ThemedViewModelBase theme;
    [ObservableProperty] private UTexture texture;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ClampedTextureHeight))]
    [NotifyPropertyChangedFor(nameof(ClampedTextureWidth))]
    private Bitmap textureSource;
    public int ClampedTextureWidth => Math.Clamp(TextureSource.PixelSize.Width, 640, 1280);
    public int ClampedTextureHeight => Math.Clamp(TextureSource.PixelSize.Height + 30, 360, 720);

    public TexturePreviewViewModel()
    {
        Theme = ThemeVM;
    }

    public void SetTexture(UTexture texture)
    {
        Texture = texture;
        UpdateChannels();
    }

    public Image<Rgba32> GetBitmap()
    {
        var decodedTexture = Texture.DecodeImageSharp();
        if (decodedTexture is null) return null;

        if (Texture is UTextureCube)
        {
            // todo fix decodedTexture = decodedTexture.ToPanorama();
        }

        return decodedTexture;
    }

    public void UpdateChannels()
    {
        var bitmap = GetBitmap();
        bitmap.Mutate(mutator => mutator.ProcessPixelRowsAsVector4(row =>
        {
            for (var i = 0; i < row.Length; i++)
            {
                var pixel = row[i];
                if (!(UseRedChannel || UseGreenChannel || UseBlueChannel) && UseAlphaChannel)
                {
                    row[i] = new Vector4(pixel.W, pixel.W, pixel.W, 1);
                }
                else if (UseRedChannel && !(UseGreenChannel || UseBlueChannel))
                {
                    row[i] = new Vector4(pixel.X, pixel.X, pixel.X, UseAlphaChannel ? pixel.W : 1);
                }
                else if (UseGreenChannel && !(UseRedChannel || UseBlueChannel))
                {
                    row[i] = new Vector4(pixel.Y, pixel.Y, pixel.Y, UseAlphaChannel ? pixel.W : 1);
                }
                else if (UseBlueChannel && !(UseRedChannel || UseGreenChannel))
                {
                    row[i] = new Vector4(pixel.Y, pixel.Y, pixel.Y, UseAlphaChannel ? pixel.W : 1);
                }
                else
                {
                    row[i] = new Vector4
                    {
                        X = UseRedChannel ? pixel.X : 0,
                        Y = UseGreenChannel ? pixel.Y : 0,
                        Z = UseBlueChannel ? pixel.Z : 0,
                        W = UseAlphaChannel ? pixel.W : 1
                    };
                }
            }
        }));

        
        TextureSource = bitmap.ToBitmap();
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        switch (e.PropertyName)
        {
            case nameof(UseRedChannel):
            case nameof(UseGreenChannel):
            case nameof(UseBlueChannel):
            case nameof(UseAlphaChannel):
            {
                UpdateChannels();
                break;
            }
        }
    }
}