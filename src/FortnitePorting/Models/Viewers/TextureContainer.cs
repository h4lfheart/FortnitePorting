using System.ComponentModel;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using DynamicData;
using FortnitePorting.Extensions;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Viewers;

public partial class TextureContainer : ObservableObject
{
    [ObservableProperty] private UTexture _texture;
    [ObservableProperty] private string _textureName = string.Empty;
    [ObservableProperty] private int _targetMipIndex;
    [ObservableProperty] private int _minimumMip;
    [ObservableProperty] private int _maximumMip;
    [ObservableProperty] private int _targetLayerIndex;
    [ObservableProperty] private int _maximumLayer;
    
    [ObservableProperty] private WriteableBitmap _originalBitmap;
    [ObservableProperty] private WriteableBitmap _displayBitmap;

    [ObservableProperty] private bool _showRedChannel = true;
    [ObservableProperty] private bool _showGreenChannel = true;
    [ObservableProperty] private bool _showBlueChannel = true;
    [ObservableProperty] private bool _showAlphaChannel = true;
    
    public void Update()
    {
        ShowRedChannel = true;
        ShowGreenChannel = true;
        ShowBlueChannel = true;
        ShowAlphaChannel = true;
        
        if (Texture.PlatformData.Mips.Length > 0)
        {
            var firstMip = Texture.GetFirstMip();
            MinimumMip = Texture.PlatformData.Mips.IndexOf(firstMip);
            MaximumMip = Texture.PlatformData.Mips.Length - 1;
            MaximumLayer = Texture.PlatformData.Mips[MinimumMip].SizeZ - 1;
        
            if (TargetMipIndex < MinimumMip || TargetMipIndex > MaximumMip)
                TargetMipIndex = MinimumMip;
        }
        else
        {
            MinimumMip = 0;
            MaximumMip = 0;
            MaximumLayer = 0;
            TargetMipIndex = 0;
            TargetLayerIndex = 0;
        }
        
        
        UpdateTextureInfo();
        UpdateBitmap();
    }

    private void UpdateTextureInfo()
    {
        var bitmap = Texture is UTexture2DArray ? UpdateTexture2DArray() : UpdateTexture2D();
        
        if (bitmap is null) return;

        if (Texture is UTextureCube) bitmap = bitmap.ToPanorama();

        OriginalBitmap = bitmap.ToWriteableBitmap();
    }

    private CTexture? UpdateTexture2D()
    {
        return Texture.PlatformData.Mips.Length > 0
            ? Texture.DecodeMip(TargetMipIndex)
            : Texture.Decode();
    }

    private CTexture? UpdateTexture2DArray()
    {
        if (Texture is not UTexture2DArray textureArray) return null;
        
        var textures = Texture.PlatformData.Mips.Length > 0
            ? textureArray.DecodeTextureArray(TargetMipIndex)
            : textureArray.DecodeTextureArray();
        if (textures == null || textures.Length == 0) return null;
        
        return textures.Length < TargetLayerIndex ? textures[0] : textures[TargetLayerIndex];
    }

    private unsafe void UpdateBitmap()
    {
        if (OriginalBitmap is null) return;

       var bitmap = new WriteableBitmap(OriginalBitmap.PixelSize, OriginalBitmap.Dpi, PixelFormat.Bgra8888, AlphaFormat.Unpremul);

        using (var framebuffer = bitmap.Lock())
        {
            OriginalBitmap.CopyPixels(framebuffer, AlphaFormat.Unpremul);

            var enabledColorChannelCount = new[] { ShowRedChannel, ShowGreenChannel, ShowBlueChannel }.Count(boolean => boolean);
            var width = bitmap.PixelSize.Width;
            var height = bitmap.PixelSize.Height;
            var rowBytes = framebuffer.RowBytes;
            var ptr = (byte*) framebuffer.Address;

            for (var y = 0; y < height; y++)
            {
                var row = ptr + y * rowBytes;
                for (var x = 0; x < width; x++)
                {
                    var colorIndex = x * 4;

                    var blue = row[colorIndex + 0];
                    var green = row[colorIndex + 1];
                    var red = row[colorIndex + 2];

                    switch (enabledColorChannelCount)
                    {
                        case 1:
                        {
                            byte targetColor = 0;
                            if (ShowRedChannel)
                                targetColor = red;
                            else if (ShowGreenChannel)
                                targetColor = green;
                            else if (ShowBlueChannel)
                                targetColor = blue;

                            row[colorIndex + 0] = targetColor;
                            row[colorIndex + 1] = targetColor;
                            row[colorIndex + 2] = targetColor;

                            if (!ShowAlphaChannel) row[colorIndex + 3] = 255;
                            break;
                        }
                        case 0 when ShowAlphaChannel:
                        {
                            var alpha = row[colorIndex + 3];
                            row[colorIndex + 0] = alpha;
                            row[colorIndex + 1] = alpha;
                            row[colorIndex + 2] = alpha;
                            row[colorIndex + 3] = 255;
                            break;
                        }
                        default:
                        {
                            if (!ShowBlueChannel) row[colorIndex] = 0;
                            if (!ShowGreenChannel) row[colorIndex + 1] = 0;
                            if (!ShowRedChannel) row[colorIndex + 2] = 0;
                            if (!ShowAlphaChannel) row[colorIndex + 3] = 255;
                            break;
                        }
                    }
                }
            }
        }

        DisplayBitmap = bitmap;
    }
    
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        
        switch (e.PropertyName)
        {
            case nameof(ShowRedChannel):
            case nameof(ShowGreenChannel):
            case nameof(ShowBlueChannel):
            case nameof(ShowAlphaChannel):
            {
                UpdateBitmap();
                break;
            }

            case nameof(TargetMipIndex):
            case nameof(TargetLayerIndex):
            {
                UpdateTextureInfo();
                UpdateBitmap();
                break;
            }
        }
    }
}