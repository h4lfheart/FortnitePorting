using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Application;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Models.Clipboard;
using FortnitePorting.Shared.Extensions;
using SkiaSharp;
using SkiaExtensions = FortnitePorting.Extensions.SkiaExtensions;

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
    
    public bool Match(string filter, bool useRegex = false)
    {
        if (useRegex)
        {
            return CreationData switch
            {
                AssetItemCreationArgs assetArgs => Regex.IsMatch(assetArgs.DisplayName, filter)
                                                   ||  Regex.IsMatch(assetArgs.Object.Name, filter),
                CustomAssetItemCreationArgs creationArgs =>  Regex.IsMatch(creationArgs.DisplayName, filter),
                _ => true
            };
        }
        
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
        Info.Message("Unsupported Asset", "Cannot copy the path of this type of asset.");
    }

    [RelayCommand]
    public virtual async Task PreviewProperties()
    {
        Info.Message("Unsupported Asset", "Cannot view the properties of this type of asset.");
    }
    
    [RelayCommand]
    public virtual async Task SendToUser()
    {
        Info.Message("Unsupported Asset", "Cannot send this type of asset to others.");
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
        if (AppSettings.Application.FavoriteAssets.Add(id))
        {
            IsFavorite = true;
        }
        else
        {
            AppSettings.Application.FavoriteAssets.Remove(id);
            IsFavorite = false;
        }
    }
}