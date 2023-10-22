using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Services.Endpoints.Models;

namespace FortnitePorting.Controls.Home;

public partial class FeaturedArtItem : UserControl
{
    public string FeaturedArtistText { get; set; }
    public string FeaturedArtURL { get; set; }
    
    public FeaturedArtItem(FeaturedResponse featured)
    {
        InitializeComponent();

        FeaturedArtistText = $"Created By: {featured.Artist}";
        FeaturedArtURL = featured.ImageURL;
    }
}