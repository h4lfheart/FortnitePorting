using System;
using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Shared.Extensions;

namespace FortnitePorting.Models.Assets;

public partial class AssetStyleInfo : ObservableObject
{
    [ObservableProperty] private string _channelName;
    [ObservableProperty] private int _selectedStyleIndex;
    [ObservableProperty, NotifyPropertyChangedFor(nameof(IsVisible))] private ObservableCollection<AssetStyleData> _styleDatas = [];
    public bool IsVisible => StyleDatas.Count > 1;
    
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
}