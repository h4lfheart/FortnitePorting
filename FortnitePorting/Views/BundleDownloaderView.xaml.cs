using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class BundleDownloaderView
{
    public BundleDownloaderView()
    {
        InitializeComponent();
        AppVM.BundleDownloaderVM = new BundleDownloaderViewModel();
        DataContext = AppVM.BundleDownloaderVM;
    }
}