using Avalonia.Controls;
using FortnitePorting.Models.API.Responses;

namespace FortnitePorting.Controls.Home;

public partial class NewsControl : UserControl
{
    public NewsResponse Response { get; set; }
    
    public NewsControl(NewsResponse response)
    {
        InitializeComponent();
        Response = response;
    }
}