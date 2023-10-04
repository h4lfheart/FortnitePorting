using System;
using System.IO;
using AsyncImageLoader;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using FortnitePorting.Services;
using FortnitePorting.Services.Endpoints.Models;

namespace FortnitePorting.Controls;

public partial class ChangelogItem : UserControl
{
    public string Title { get; set; }
    public string PublishDate { get; set; }
    public string Text { get; set; }
    public Bitmap Image { get; set; }
    public string[] Tags { get; set; }
    
    public ChangelogItem(ChangelogResponse changelog)
    {
        InitializeComponent();

        Title = changelog.Title;
        PublishDate = changelog.PublishTime.ToString("d");
        Text = changelog.Text;
        ImageLoader.SetSource(ThumbnailImage, changelog.ImageURL);
    }
}