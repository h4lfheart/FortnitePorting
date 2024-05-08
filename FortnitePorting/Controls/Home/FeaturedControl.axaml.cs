using Avalonia.Controls;
using FortnitePorting.Models.API.Responses;

namespace FortnitePorting.Controls.Home;

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