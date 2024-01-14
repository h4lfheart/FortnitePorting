using System.Linq;
using Avalonia.Interactivity;
using FortnitePorting.Framework;
using FortnitePorting.ViewModels;

namespace FortnitePorting.Views;

public partial class HomeView : ViewBase<HomeViewModel>
{
    public HomeView()
    {
        InitializeComponent();
    }

    // fixes stupid avalonia issue with transitioning content control parents
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        ViewModel.CurrentFeaturedArt = ViewModel.FeaturedArt.FirstOrDefault();
        ViewModel.CurrentFeaturedArtIndex = 0;
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        ViewModel.CurrentFeaturedArt = null;
        ViewModel.CurrentFeaturedArtIndex = 0;
    }
}