using System;
using System.IO;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using FortnitePorting.Extensions;
using FortnitePorting.Services;
using FortnitePorting.Services.Endpoints.Models;

namespace FortnitePorting.Controls;

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