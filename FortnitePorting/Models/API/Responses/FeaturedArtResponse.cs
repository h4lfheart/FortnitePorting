using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class FeaturedArtResponse
{
    [JsonProperty("title")] public string Title { get; set; }
    [JsonProperty("artist_name")] public string Artist { get; set; }
    [JsonProperty("social_url")] public string Social { get; set; }
    [JsonProperty("image_url")] public string Image { get; set; }
    
}