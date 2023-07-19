using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports.Texture;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.ViewModels;

public partial class ImageViewerViewModel : ObservableObject
{
    [ObservableProperty] private string imageName;
    [ObservableProperty] private int imageWidth;
    [ObservableProperty] private int imageHeight;
    [ObservableProperty] private BitmapSource imageSource;

    public void Initialize(UTexture2D texture, string? title = null)
    {
        ImageName = title ?? texture.Name;
        ImageSource = texture.Decode().ToBitmapImage();
        ImageWidth = ImageSource.PixelWidth;
        ImageHeight = ImageSource.PixelHeight;
    }
}