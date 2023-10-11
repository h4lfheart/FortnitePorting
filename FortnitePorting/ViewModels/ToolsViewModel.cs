using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse_Conversion.Textures;
using FortnitePorting.Framework;
using SkiaSharp;

namespace FortnitePorting.ViewModels;

public partial class ToolsViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap minimapSource;

    public override async Task Initialize()
    {
        if (CUE4ParseVM.Minimap is not null) 
            MinimapSource = new Bitmap(CUE4ParseVM.Minimap.Decode()!.Encode(SKEncodedImageFormat.Png, 100).AsStream());
    }
}