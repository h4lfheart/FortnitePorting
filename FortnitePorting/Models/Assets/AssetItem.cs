using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.GameplayTags;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.Multiplayer.Models;
using FortnitePorting.Multiplayer.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared;
using FortnitePorting.Shared.Extensions;
using SkiaSharp;
using SkiaExtensions = FortnitePorting.Shared.Extensions.SkiaExtensions;

namespace FortnitePorting.Models.Assets;


public partial class AssetItem : ObservableObject
{
    public AssetItemCreationArgs CreationData { get; set; }
    public Guid Guid { get; set; }

    [ObservableProperty] private bool _isFavorite;

    public EFortRarity Rarity { get; set; }
    public int Season { get; set; }
    public UFortItemSeriesDefinition? Series { get; set; }
    public WriteableBitmap DisplayImage { get; set; }
    public WriteableBitmap IconDisplayImage { get; set; }

    public float DisplayWidth { get; set; } = 64;
    public float DisplayHeight { get; set; } = 80;

    private static SKColor InnerBackgroundColor = SKColor.Parse("#50C8FF");
    private static SKColor OuterBackgroundColor = SKColor.Parse("#1B7BCF");
    
    public AssetItem(AssetItemCreationArgs args)
    {
        CreationData = args;
        Guid = Guid.NewGuid();

        Rarity = CreationData.Object.GetOrDefault("Rarity", EFortRarity.Uncommon);
        
        var seasonTag = CreationData.GameplayTags?.GetValueOrDefault("Cosmetics.Filter.Season.")?.Text;
        Season = int.TryParse(seasonTag?.SubstringAfterLast("."), out var seasonNumber) ? seasonNumber : int.MaxValue;
        
        var seriesComponent = CreationData.Object.TryGetFortComponentByType("FortItemComponent_Series");
        Series = seriesComponent?.GetOrDefault<UFortItemSeriesDefinition?>("Series");
        
        var iconBitmap = CreationData.Icon.Decode()!;
        IconDisplayImage = iconBitmap.ToWriteableBitmap();
        DisplayImage = CreateDisplayImage(iconBitmap).ToWriteableBitmap();
    }

    public SKBitmap CreateDisplayImage(SKBitmap iconBitmap)
    {
        var bitmap = new SKBitmap(128, 160, iconBitmap.ColorType, SKAlphaType.Opaque);
        using (var canvas = new SKCanvas(bitmap))
        {
            var colors = Series?.Colors ?? CUE4ParseVM.RarityColors[(int) Rarity];
            // background
            var backgroundRect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
            if (Series?.BackgroundTexture.LoadOrDefault<UTexture2D>() is { } seriesBackground)
            {
                canvas.DrawBitmap(seriesBackground.Decode(), backgroundRect);
            }
            else if (!CreationData.HideRarity)
            {
                var backgroundPaint = new SKPaint { Shader = SkiaExtensions.RadialGradient(bitmap.Height, colors.Color1, colors.Color3) };
                canvas.DrawRect(backgroundRect, backgroundPaint);
            }
            else
            {
                var backgroundPaint = new SKPaint { Shader = SkiaExtensions.RadialGradient(bitmap.Height, InnerBackgroundColor, OuterBackgroundColor) };
                canvas.DrawRect(backgroundRect, backgroundPaint);
            }

            if (CreationData.ExportType is EExportType.Prop or EExportType.Prefab)
            {
                canvas.DrawBitmap(iconBitmap, backgroundRect with { Left = -16, Right = bitmap.Width + 16 });
            }
            else
            {
                canvas.DrawBitmap(iconBitmap, backgroundRect with { Left = -8, Right = bitmap.Width + 8, Bottom = bitmap.Height - 16 });
            }

            if (!CreationData.HideRarity)
            {
                var coolRectPaint = new SKPaint { Shader = SkiaExtensions.LinearGradient(bitmap.Width, true, colors.Color1, colors.Color2) };
                coolRectPaint.Color = coolRectPaint.Color.WithAlpha((byte) (0.75 * byte.MaxValue));

                canvas.RotateDegrees(-4);
                canvas.DrawRect(new SKRect(-16, bitmap.Height - 12, bitmap.Width + 16, bitmap.Height + 16), coolRectPaint);
                canvas.RotateDegrees(4);
            }
            
        }

        return bitmap;
    }
    
    public bool Match(string filter)
    {
        return MiscExtensions.Filter(CreationData.DisplayName, filter) || MiscExtensions.Filter(CreationData.Object.Name, filter);
    }
    
    [RelayCommand]
    public async Task CopyPath()
    {
        await Clipboard.SetTextAsync(CreationData.Object.GetPathName());
    }
    
    [RelayCommand]
    public async Task SendToUser()
    {
        var users = ChatVM.Users.Select(user => user.DisplayName);
        var comboBox = new ComboBox
        {
            ItemsSource = users,
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        
        var dialog = new ContentDialog
        {
            Title = $"Export \"{CreationData.DisplayName}\" to User",
            Content = comboBox,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Send",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                var targetUser = ChatVM.Users.FirstOrDefault(user => user.DisplayName.Equals(comboBox.SelectionBoxItem));
                if (targetUser is null) return;
                
                await OnlineService.Send(new ExportPacket(CreationData.Object.GetPathName()), new MetadataBuilder().With("Target", targetUser.Guid));
                AppWM.Message("Export Sent", $"Successfully sent {CreationData.DisplayName} to {targetUser.DisplayName}");
            })
        };

        await dialog.ShowAsync();
    }
}

public class AssetItemCreationArgs
{
    public required UObject Object { get; set; }
    public required UTexture2D Icon { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required EExportType ExportType { get; set; }
    public bool IsHidden { get; set; } = false;
    public bool HideRarity { get; set; } = false;
    public FGameplayTagContainer? GameplayTags { get; set; }
}