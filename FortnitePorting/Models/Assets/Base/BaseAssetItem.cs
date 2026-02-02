using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FortnitePorting.Extensions;
using FortnitePorting.Models.Assets.Asset;
using FortnitePorting.Models.Assets.Custom;
using FortnitePorting.Models.Clipboard;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using SkiaSharp;
using SkiaExtensions = FortnitePorting.Extensions.SkiaExtensions;

namespace FortnitePorting.Models.Assets.Base;

public abstract partial class BaseAssetItem : ObservableObject
{
    public SettingsService Settings => AppSettings;
    
    [ObservableProperty] private bool _isFavorite;
    
    public Guid Id { get; set; }
    public virtual BaseAssetItemCreationArgs CreationData { get; set; }

    [ObservableProperty] private WriteableBitmap? _iconDisplayImage = null;
    [ObservableProperty] private WriteableBitmap? _backgroundImage = null;

    private SKColor InnerBackgroundColor { get; set; } = SKColor.Parse("#50C8FF");
    private SKColor OuterBackgroundColor { get; set; } = SKColor.Parse("#1B7BCF");
    
    public bool Match(string filter, bool useRegex = false)
    {
        if (useRegex)
        {
            return this switch
            {
                AssetItem assetItem => Regex.IsMatch(assetItem.CreationData.DisplayName, filter)
                                                    || Regex.IsMatch(assetItem.CreationData.Object.Name, filter)
                                                    || (assetItem.SetName is not null && Regex.IsMatch(assetItem.SetName, filter))
                                                    || (assetItem.Series is not null && Regex.IsMatch(assetItem.Series.DisplayName.Text, filter)),
                CustomAssetItem customAssetItem =>  Regex.IsMatch(customAssetItem.CreationData.DisplayName, filter),
                _ => true
            };
        }
        
        return this switch
        {
            AssetItem assetItem => MiscExtensions.Filter(assetItem.CreationData.DisplayName, filter)
                                   || MiscExtensions.Filter(assetItem.CreationData.Object.Name, filter)
                                   || (assetItem.SetName is not null && MiscExtensions.Filter(assetItem.SetName, filter))
                                   || (assetItem.Series is not null && MiscExtensions.Filter(assetItem.Series.DisplayName.Text, filter)),
            CustomAssetItem customAssetItem => MiscExtensions.Filter(customAssetItem.CreationData.DisplayName, filter),
            _ => true
        };
    }

    protected virtual WriteableBitmap CreateBackgroundImage()
    {
        var bitmap = new SKBitmap(128, 160, SKColorType.Rgba8888, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bitmap))
        {
            var backgroundRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
            var backgroundPaint = new SKPaint { Shader = SkiaExtensions.RadialGradient(bitmap.Height, InnerBackgroundColor, OuterBackgroundColor) };
            canvas.DrawRect(backgroundRect, backgroundPaint);
        }

        return bitmap.ToWriteableBitmap();
    }
    
    [RelayCommand]
    public virtual async Task NavigateTo()
    {
        Info.Message("Unsupported Asset", "Cannot navigate to this type of asset.");
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
        await AvaloniaClipboard.SetImageAsync(IconDisplayImage);
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