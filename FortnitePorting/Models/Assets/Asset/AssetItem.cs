using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.GameTypes.FN.Enums;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using FluentAvalonia.UI.Controls;
using FortnitePorting.Application;
using FortnitePorting.Export;
using FortnitePorting.Models.Fortnite;
using FortnitePorting.OnlineServices.Models;
using FortnitePorting.OnlineServices.Packet;
using FortnitePorting.Services;
using FortnitePorting.Shared.Extensions;
using FortnitePorting.Shared.Models.Clipboard;
using FortnitePorting.Windows;
using Newtonsoft.Json;
using SkiaSharp;
using SkiaExtensions = FortnitePorting.Shared.Extensions.SkiaExtensions;

namespace FortnitePorting.Models.Assets.Asset;


public partial class AssetItem : Base.BaseAssetItem
{
    public new AssetItemCreationArgs CreationData
    {
        get => (AssetItemCreationArgs) base.CreationData;
        private init => base.CreationData = value;
    }

    public EFortRarity Rarity { get; set; }
    public int Season { get; set; }
    public UFortItemSeriesDefinition? Series { get; set; }
    

    private static SKColor InnerBackgroundColor = SKColor.Parse("#50C8FF");
    private static SKColor OuterBackgroundColor = SKColor.Parse("#1B7BCF");

    private static ConcurrentDictionary<string, UFortItemSeriesDefinition> SeriesCache = [];
    
    public AssetItem(AssetItemCreationArgs args)
    {
        Id = Guid.NewGuid();
        CreationData = args;

        IsFavorite = AppSettings.Current.FavoriteAssets.Contains(CreationData.Object.GetPathName());

        Rarity = CreationData.Object.GetOrDefault("Rarity", EFortRarity.Uncommon);
        
        var seasonTag = CreationData.GameplayTags?.GetValueOrDefault("Cosmetics.Filter.Season.")?.Text;
        Season = int.TryParse(seasonTag?.SubstringAfterLast("."), out var seasonNumber) ? seasonNumber : int.MaxValue;

        if (CreationData.Object.GetDataListItem<FPackageIndex>("Series") is { } seriesPackage)
        {
            Series = SeriesCache!.GetOrAdd(seriesPackage.Name,
                () => seriesPackage.Load<UFortItemSeriesDefinition>());
        }
        
        var iconBitmap = CreationData.Icon.Decode()!;
        IconDisplayImage = iconBitmap.ToWriteableBitmap();
        DisplayImage = CreateDisplayImage(iconBitmap).ToWriteableBitmap();
    }

    protected sealed override SKBitmap CreateDisplayImage(SKBitmap iconBitmap)
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

            if (CreationData.HideRarity)
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
    
    public override async Task CopyPath()
    {
        await Clipboard.SetTextAsync(CreationData.Object.GetPathName());
    }

    public override async Task PreviewProperties()
    {
        var assets = await CUE4ParseVM.Provider.LoadAllObjectsAsync(Exporter.FixPath(CreationData.Object.GetPathName()));
        var json = JsonConvert.SerializeObject(assets, Formatting.Indented);
        PropertiesPreviewWindow.Preview(CreationData.Object.Name, json);
    }
    
    public override async Task SendToUser()
    {
        var xaml =
            """
                <ContentControl xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:ext="clr-namespace:FortnitePorting.Shared.Extensions;assembly=FortnitePorting.Shared"
                            xmlns:shared="clr-namespace:FortnitePorting.Shared;assembly=FortnitePorting.Shared">
                    <StackPanel HorizontalAlignment="Stretch">
                        <ComboBox x:Name="UserSelectionBox" SelectedIndex="0" Margin="{ext:Space 0, 1, 0, 0}"
                                  ItemsSource="{Binding Users}"
                                  HorizontalAlignment="Stretch">
                            <ComboBox.ItemContainerTheme>
                                <ControlTheme x:DataType="ext:EnumRecord" TargetType="ComboBoxItem" BasedOn="{StaticResource {x:Type ComboBoxItem}}">
                                    <Setter Property="IsEnabled" Value="{Binding !IsDisabled}"/>
                                </ControlTheme>
                            </ComboBox.ItemContainerTheme>
                        </ComboBox>
                        <TextBox x:Name="MessageBox" Watermark="Message (Optional)" TextWrapping="Wrap" Margin="{ext:Space 0, 1, 0, 0}"/>
                    </StackPanel>
                </ContentControl>
            """;
                    
        var content = xaml.CreateXaml<ContentControl>(new
        {
            Users = ChatVM.Users.Select(user => user.DisplayName)
        });
                    
        var comboBox = content.FindControl<ComboBox>("UserSelectionBox");
        comboBox.SelectedIndex = 0;
        var messageBox = content.FindControl<TextBox>("MessageBox");
        
        var dialog = new ContentDialog
        {
            Title = $"Export \"{CreationData.DisplayName}\" to User",
            Content = content,
            CloseButtonText = "Cancel",
            PrimaryButtonText = "Send",
            PrimaryButtonCommand = new RelayCommand(async () =>
            {
                if (messageBox?.Text is not { } message) return;
                
                var targetUser = ChatVM.Users.FirstOrDefault(user => user.DisplayName.Equals(comboBox!.SelectionBoxItem));
                if (targetUser is null) return;
                
                await OnlineService.Send(new ExportPacket(CreationData.Object.GetPathName(), message), new MetadataBuilder().With("Target", targetUser.Guid));
                AppWM.Message("Export Sent", $"Successfully sent {CreationData.DisplayName} to {targetUser.DisplayName}");
            })
        };

        await dialog.ShowAsync();
    }
    
    public override async Task CopyIcon(bool withBackground = false)
    {
        await AvaloniaClipboard.SetImageAsync(withBackground ? DisplayImage : IconDisplayImage);
    }
    
    public override void Favorite()
    {
        var path = CreationData.Object.GetPathName();
        if (AppSettings.Current.FavoriteAssets.Add(path))
        {
            IsFavorite = true;
        }
        else
        {
            AppSettings.Current.FavoriteAssets.Remove(path);
            IsFavorite = false;
        }
    }
}