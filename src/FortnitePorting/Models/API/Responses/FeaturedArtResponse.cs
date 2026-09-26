using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class FeaturedArtResponse
{
    [JsonProperty("entries")] public FeaturedArtEntry[] Entries { get; set; } = [];
}

public class FeaturedArtEntry
{
    [JsonProperty("title")] public string Title { get; set; }
    [JsonProperty("artistName")] public string Artist { get; set; }
    [JsonProperty("socialUrl")] public string Social { get; set; }
    [JsonProperty("imageUrl")] public string Image { get; set; }
}