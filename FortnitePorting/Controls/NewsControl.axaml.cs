using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FortnitePorting.Models.API.Responses;

namespace FortnitePorting.Controls;

public partial class NewsControl : UserControl
{
    public NewsResponse Response { get; set; }
    
    public NewsControl(NewsResponse response)
    {
        InitializeComponent();
        Response = response;
    }
}