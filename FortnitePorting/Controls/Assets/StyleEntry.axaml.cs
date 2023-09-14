using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Extensions;

namespace FortnitePorting.Controls.Assets;

public partial class StyleEntry : UserControl
{
    public string StyleName { get; set; }
    public Bitmap StylePreviewImage { get; set; }
    public FStructFallback StyleInfo { get; set; }
    
    public StyleEntry(FStructFallback styleInfo, Bitmap previewImage)
    {
        InitializeComponent();
        
        StylePreviewImage = previewImage;
        StyleInfo = styleInfo;
        StyleName = styleInfo.GetOrDefault("VariantName", new FText("Unnamed")).Text.ToLower().TitleCase();
        if (string.IsNullOrWhiteSpace(StyleName)) StyleName = "Unnamed";

    }
}