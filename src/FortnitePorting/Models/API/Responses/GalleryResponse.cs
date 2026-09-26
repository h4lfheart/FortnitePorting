using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public record GalleryResponse
{
    [JsonProperty("baseUrl")] public string BaseUrl { get; set; }
    [JsonProperty("fileNames")] public List<string> FileNames { get; set; }
}