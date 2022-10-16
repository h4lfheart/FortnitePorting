using System;
using System.Windows.Controls;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using SkiaSharp;

namespace FortnitePorting.Views.Controls;

public partial class StyleSelector
{
    public string ChannelName;
    
    
    public StyleSelector(string channelName, FStructFallback[] options)
    {
        InitializeComponent();
        DataContext = this;
        
        ChannelName = channelName;
        foreach (var option in options)
        {
            if (!option.TryGetValue(out UTexture2D previewTexture, "PreviewImage")) return;
            var previewBitmap = previewTexture.Decode();
            if (previewBitmap is null) continue;
            
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