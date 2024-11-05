using System;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Application;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models.Clipboard;
using SkiaSharp;
using SkiaExtensions = FortnitePorting.Shared.Extensions.SkiaExtensions;

namespace FortnitePorting.Models.Assets.Base;

public abstract partial class BaseAssetItem : ObservableObject
{
    
    [ObservableProperty] private bool _isFavorite;
    
    public Guid Id { get; set; }
    public virtual BaseAssetItemCreationArgs CreationData { get; set; }
    
    public WriteableBitmap DisplayImage { get; set; }
    public WriteableBitmap IconDisplayImage { get; set; }
    
    public float DisplayWidth { get; set; } = 64;
    public float DisplayHeight { get; set; } = 80;

    private SKColor InnerBackgroundColor { get; set; } = SKColor.Parse("#50C8FF");
    private SKColor OuterBackgroundColor { get; set; } = SKColor.Parse("#1B7BCF");
    
    public bool Match(string filter)
    {
        return CreationData switch
        {
            AssetItemCreationArgs assetArgs => MiscExtensions.Filter(assetArgs.DisplayName, filter)
                                               || MiscExtensions.Filter(assetArgs.Object.Name, filter),
            CustomAssetItemCreationArgs creationArgs => MiscExtensions.Filter(creationArgs.DisplayName, filter),
            _ => true
        };
    }

    protected virtual SKBitmap CreateDisplayImage(SKBitmap iconBitmap)
    {
        var bitmap = new SKBitmap(128, 160, iconBitmap.ColorType, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bitmap))
        {
            var backgroundRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
            var backgroundPaint = new SKPaint { Shader = SkiaExtensions.RadialGradient(bitmap.Height, InnerBackgroundColor, OuterBackgroundColor) };
            canvas.DrawRect(backgroundRect, backgroundPaint);
            canvas.DrawBitmap(iconBitmap, backgroundRect with { Left = -16, Right = bitmap.Width + 16 });
        }

        return bitmap;
    }
    
    [RelayCommand]
    public virtual async Task CopyPath()
    {
        AppWM.Message("Unsupported Asset", "Cannot copy the path of this type of asset.");
    }

    [RelayCommand]
    public virtual async Task PreviewProperties()
    {
        AppWM.Message("Unsupported Asset", "Cannot view the properties of this type of asset.");
    }
    
    [RelayCommand]
    public virtual async Task SendToUser()
    {
        AppWM.Message("Unsupported Asset", "Cannot send this type of asset to others.");
    }
    
    [RelayCommand]
    public virtual async Task CopyIcon(bool withBackground = false)
    {
        await AvaloniaClipboard.SetImageAsync(withBackground ? DisplayImage : IconDisplayImage);
    }
    
    [RelayCommand]
    public virtual void Favorite()
    {
        var id = CreationData.ID;
        if (AppSettings.Current.FavoriteAssets.Add(id))
        {
            IsFavorite = true;
        }
        else
        {
            AppSettings.Current.FavoriteAssets.Remove(id);
            IsFavorite = false;
        }
    }
}