using System.ComponentModel;
using System.Linq;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using DynamicData;
using FortnitePorting.Extensions;

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
        FTexture2DMipMap? mip = null;
        if (Texture.PlatformData.Mips.Length > 0)
            mip = Texture.PlatformData.Mips[TargetMipIndex];

        var bitmap = Texture is UTexture2DArray ? UpdateTexture2DArray(mip) : UpdateTexture2D(mip);
        
        if (bitmap is null) return;

        if (Texture is UTextureCube) bitmap = bitmap.ToPanorama();

        OriginalBitmap = bitmap.ToWriteableBitmap();

    }

    private CTexture? UpdateTexture2D(FTexture2DMipMap? mip = null)
    {
        return mip != null ? Texture.Decode(mip) : Texture.Decode();
    }

    private CTexture? UpdateTexture2DArray(FTexture2DMipMap? mip = null)
    {
        var textures = ((UTexture2DArray)Texture).DecodeTextureArray(mip);
        if (textures == null || textures.Length == 0) return null;
        
        return textures.Length < TargetLayerIndex ? textures[0] : textures[TargetLayerIndex];
    }

    private unsafe void UpdateBitmap()
    {
        DisplayBitmap = new WriteableBitmap(OriginalBitmap.PixelSize, OriginalBitmap.Dpi, OriginalBitmap.Format, OriginalBitmap.AlphaFormat);
        
        var framebuffer = DisplayBitmap.Lock();
        OriginalBitmap.CopyPixels(framebuffer, AlphaFormat.Unpremul);

        var enabledColorChannelCount = new[] { ShowRedChannel, ShowGreenChannel, ShowBlueChannel }.Count(boolean => boolean);
        
        var ptr = (byte*) framebuffer.Address;
        for (var x = 0; x < DisplayBitmap.PixelSize.Width; x++)
        {
            for (var y = 0; y < DisplayBitmap.PixelSize.Height; y++)
            {
                var colorIndex = (y * DisplayBitmap.PixelSize.Width + x) * 4;

                // todo maybe convert all to rgba and not hardcode for bgra
                var red = ptr[colorIndex + 2];
                var green = ptr[colorIndex + 1];
                var blue = ptr[colorIndex + 0];
                
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
                    
                        for (var colPtr = 0; colPtr < 3; colPtr++)
                        {
                            ptr[colorIndex + colPtr] = targetColor;
                        }

                        if (!ShowAlphaChannel) ptr[colorIndex + 3] = 255;
                        break;
                    }
                    case 0 when ShowAlphaChannel:
                    {
                        var alpha = ptr[colorIndex + 3];
                        for (var colPtr = 0; colPtr < 3; colPtr++)
                        {
                            ptr[colorIndex + colPtr] = alpha;
                        }

                        ptr[colorIndex + 3] = 255;
                        break;
                    }
                    default:
                    {
                        if (!ShowBlueChannel) ptr[colorIndex] = 0;
                        if (!ShowGreenChannel) ptr[colorIndex + 1] = 0;
                        if (!ShowRedChannel) ptr[colorIndex + 2] = 0;
                        if (!ShowAlphaChannel) ptr[colorIndex + 3] = 255;
                        break;
                    }
                }
            }
           
        }
        
        framebuffer.Dispose();
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