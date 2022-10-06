using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Views.Extensions;
using SkiaSharp;

namespace FortnitePorting.Views.Controls;

public partial class StyleSelectorItem
{
    public FStructFallback OptionData;
    public string DisplayName { get; set; }
    public BitmapSource IconSource { get; set; }
    
    public StyleSelectorItem(FStructFallback option, SKBitmap previewBitmap)
    {
        InitializeComponent();
        OptionData = option;
        DisplayName = option.GetOrDefault("VariantName", new FText("Unknown Style")).Text.ToLower().TitleCase();
        IconSource = previewBitmap.ToBitmapSource();

    }
}