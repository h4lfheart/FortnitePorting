using Newtonsoft.Json;

namespace FortnitePorting.Services.Endpoints;

public class ContentBuildsResponse
{
    [JsonProperty("items")]
    public ContentItems Items;

    public class ContentItems
    {
        [JsonProperty("MANIFEST")]
        public ContentItem Manifest;
    }

    public class ContentItem
    {
        [JsonProperty("distribution")]
        public string Distribution;
        [JsonProperty("path")]
        public string Path;
    }
}