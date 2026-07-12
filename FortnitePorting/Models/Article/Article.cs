using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FortnitePorting.Models.Article;

[ObservableObject]
public partial class Article
{
    public string Id { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    [ObservableProperty] private DateTime _timestamp;
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private string _tag = string.Empty;
    [ObservableProperty] private ObservableCollection<ArticleSection> _sections = [];
}
