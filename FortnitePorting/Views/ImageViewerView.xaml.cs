using System.Windows;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class ImageViewerView
{
    public ImageViewerView()
    {
        InitializeComponent();
        AppVM.ImageVM = new ImageViewerViewModel();
        DataContext = AppVM.ImageVM;
    }
}