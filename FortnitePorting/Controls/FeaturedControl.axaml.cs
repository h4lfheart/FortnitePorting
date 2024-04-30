using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using FortnitePorting.Models.API.Responses;

namespace FortnitePorting.Controls;

public partial class FeaturedControl : UserControl
{
    public FeaturedResponse Response { get; set; }
    
    public FeaturedControl(FeaturedResponse response)
    {
        InitializeComponent();
        Response = response;
    }

    public void LaunchSocial()
    {
        Launch(Response.Social);
    }
}