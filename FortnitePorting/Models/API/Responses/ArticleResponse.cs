using System;
using System.Collections.Generic;
using FortnitePorting.Models.Article;
using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class ArticlesResponse
{
    [JsonProperty("entries")] public List<ArticleResponseEntry> Entries { get; set; } = [];
}

public class ArticleResponseEntry
{
    [JsonProperty("id")] public string Id { get; set; } = string.Empty;
    [JsonProperty("author")] public string Author { get; set; } = string.Empty;
    [JsonProperty("timestamp")] public DateTime Timestamp { get; set; }
    [JsonProperty("title")] public string Title { get; set; } = string.Empty;
    [JsonProperty("description")] public string Description { get; set; } = string.Empty;
    [JsonProperty("tag")] public string Tag { get; set; } = string.Empty;
    [JsonProperty("sections")] public List<ArticleSectionEntry> Sections { get; set; } = [];
}

public class ArticleSectionEntry
{
    [JsonProperty("type")] public EHelpSectionType Type { get; set; }
    [JsonProperty("content")] public string Content { get; set; } = string.Empty;
}

public record UploadArticleImageResponse([property: JsonProperty("path")] string Path);
