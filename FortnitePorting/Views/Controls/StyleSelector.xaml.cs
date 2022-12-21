using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using SkiaSharp;

namespace FortnitePorting.Views.Controls;

public partial class StyleSelector
{
    public string ChannelName;
    public Orientation Orientation { get; set; }
    
    
    public StyleSelector(string channelName, FStructFallback[] options, SKBitmap fallbackBitmap)
    {
        InitializeComponent();
        DataContext = this;
        
        ChannelName = channelName;
        foreach (var option in options)
        {
            var previewBitmap = fallbackBitmap;
            if (option.TryGetValue(out UTexture2D previewTexture, "PreviewImage"))
            {
                previewBitmap = previewTexture.Decode();
                if (previewBitmap is null) continue;
            }
            
            var fullBitmap = new SKBitmap(previewBitmap.Width, previewBitmap.Height, previewBitmap.ColorType, previewBitmap.AlphaType);
            using (var fullCanvas = new SKCanvas(fullBitmap))
            {
                DrawBackground(fullCanvas, Math.Max(previewBitmap.Width, previewBitmap.Height));
                fullCanvas.DrawBitmap(previewBitmap, 0, 0);
            }
            
            Options.Items.Add(new StyleSelectorItem(option, fullBitmap));
        }
        Options.SelectedIndex = 0;
    }
    
    public StyleSelector(List<AssetSelectorItem> items)
    {
        InitializeComponent();
        DataContext = this;
        Options.IsEnabled = false;
        Title.Visibility = Visibility.Collapsed;
        Orientation = Orientation.Vertical;

        foreach (var item in items)
        {
            Options.Items.Add(new TextWithIcon {Label = " " + item.DisplayName, ImageSource = item.FullSource, IconSize = 32, Foreground = Brushes.White});
        }
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Options.SelectedItem is not StyleSelectorItem selectedItem) return;
        Title.Tag = $"{ChannelName} ({selectedItem.DisplayName})";
    }
    
    private void DrawBackground(SKCanvas canvas, int size)
    {
        SKShader BackgroundShader(params SKColor[] colors)
        {;
            return SKShader.CreateRadialGradient(new SKPoint(size / 2f, size / 2f), size / 5 * 4, colors,
                SKShaderTileMode.Clamp);
        }

        canvas.DrawRect(new SKRect(0, 0, size, size), new SKPaint
        {
            Shader = BackgroundShader(SKColor.Parse("#50C8FF"), SKColor.Parse("#1B7BCF"))
        });
    }
}