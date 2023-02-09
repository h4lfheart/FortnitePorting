using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class HeightmapView
{
    public HeightmapView()
    {
        InitializeComponent();
        AppVM.HeightmapVM = new HeightmapViewModel();
        DataContext = AppVM.HeightmapVM;
    }
}