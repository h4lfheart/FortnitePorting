using Avalonia.Controls;
using FortnitePorting.Framework.ViewModels.Endpoints.Models;
using FortnitePorting.ViewModels.Endpoints.Models;

namespace FortnitePorting.Controls.Home;

public partial class ChangelogItem : UserControl
{
    public string Title { get; set; }
    public string PublishDate { get; set; }
    public string Text { get; set; }
    public string ImageURL { get; set; }
    public string[] Tags { get; set; }

    public ChangelogItem(ChangelogResponse changelog)
    {
        InitializeComponent();

        Title = changelog.Title;
        PublishDate = changelog.PublishDate.ToString("d");
        Text = changelog.Text;
        ImageURL = changelog.ImageURL;
        Tags = changelog.Tags; //$"Tags: {changelog.Tags.CommaJoin(includeAnd: false)}";
    }
}