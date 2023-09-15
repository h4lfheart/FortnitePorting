using System;
using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using Serilog;
using SkiaSharp;

namespace FortnitePorting.Controls.Assets;

public partial class StyleItem : UserControl
{
    public string ChannelName { get; set; }
    public ObservableCollection<StyleEntry> Styles { get; set; } = new();
    
    public StyleItem(string channelName, FStructFallback[] styles, Bitmap fallbackPreviewImage)
    {
        InitializeComponent();

        ChannelName = channelName;
        
        foreach (var style in styles)
        {
            var isEmpty = style.GetOrDefault<FText?>("VariantName")?.Text.Equals("Empty", StringComparison.OrdinalIgnoreCase);
            if (!isEmpty.HasValue || isEmpty.Value) continue;
            
            var previewBitmap = fallbackPreviewImage;
            if (style.TryGetValue(out UTexture2D previewTexture, "PreviewImage"))
            {
                var imageStream = previewTexture.Decode()?.Encode(SKEncodedImageFormat.Png, 100).AsStream();
                if (imageStream is null) continue;
                
                previewBitmap = new Bitmap(imageStream);
            }
            
            Styles.Add(new StyleEntry(style, previewBitmap));
        }

        IsVisible = Styles.Count > 1;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        StylesListBox.SelectedIndex = 0;
    }
}