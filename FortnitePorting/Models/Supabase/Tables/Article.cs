using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FortnitePorting.Models.Article;
using Newtonsoft.Json;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace FortnitePorting.Models.Supabase.Tables;

[ObservableObject]
[Table("articles")]
public partial class Article : BaseModel
{
    [PrimaryKey("id")] public string Id { get; set; } = string.Empty;
    [Column("author")] public string Author { get; set; } = string.Empty;
    
    [ObservableProperty] [property: JsonProperty("timestamp")] private DateTime _timestamp;
    [ObservableProperty] [property: JsonProperty("title")] private string _title = string.Empty;
    [ObservableProperty] [property: JsonProperty("description")] private string _description = string.Empty;
    [ObservableProperty] [property: JsonProperty("tag")] private string _tag = string.Empty;
    [ObservableProperty] [property: JsonProperty("sections")] private ObservableCollection<ArticleSection> _sections = [];
}