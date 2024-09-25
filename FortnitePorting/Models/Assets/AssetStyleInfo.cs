using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Shared.Extensions;
using AssetLoader = FortnitePorting.Models.Assets.Loading.AssetLoader;

namespace FortnitePorting.Models.Assets;

public partial class AssetStyleInfo : ObservableObject
{
    [ObservableProperty] private string _channelName;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsVisible))] private ObservableCollection<BaseStyleData> _styleDatas = [];
    public bool IsVisible => StyleDatas.Count > 1;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(SelectionMode))] private bool _multiSelect = false;
    public SelectionMode SelectionMode => MultiSelect ? SelectionMode.Multiple : SelectionMode.Single;
    
    [ObservableProperty] private int _selectedStyleIndex;
    [ObservableProperty] private ObservableCollection<BaseStyleData> _selectedItems = [];
    
    
    public AssetStyleInfo(string channelName, FStructFallback[] styles, Bitmap fallbackPreviewImage)
    {
        ChannelName = channelName;
        
        foreach (var style in styles)
        {
            if (style.GetOrDefault<FText?>("VariantName") is not { } variantNameText 
                || variantNameText.Text.Equals("Empty", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var previewBitmap = fallbackPreviewImage;
            if (style.TryGetValue(out UTexture2D previewTexture, "PreviewImage"))
            {
                previewBitmap = previewTexture.Decode()!.ToWriteableBitmap();
            }

            StyleDatas.Add(new AssetStyleData(style, previewBitmap));
        }

        SelectedStyleIndex = 0;
    }
    
    public AssetStyleInfo(string channelName, IEnumerable<UObject> styles, Bitmap fallbackPreviewImage)
    {
        MultiSelect = true;
        ChannelName = channelName;
        
        foreach (var style in styles)
        {
            var previewBitmap = AssetLoader.GetIcon(style)?.Decode()?.ToWriteableBitmap() ?? fallbackPreviewImage;
            StyleDatas.Add(new ObjectStyleData(style.Name, style, previewBitmap));
        }

        SelectedStyleIndex = 0;
    }
}