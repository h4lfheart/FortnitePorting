using J = Newtonsoft.Json.JsonPropertyAttribute;

namespace FortnitePorting.Services.Endpoints.Models;

public class ContentBuildsResponse
{
    [J] public ContentItems Items;

    public class ContentItems
    {
        [J] public ContentItem Manifest;
    }

    public class ContentItem
    {
        [J] public string Distribution;
        [J] public string Path;
    }
}