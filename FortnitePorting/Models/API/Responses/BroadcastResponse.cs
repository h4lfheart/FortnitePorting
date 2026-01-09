using System;
using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class BroadcastResponse
{
    [JsonProperty("title")] public string Title { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("timestamp")] public DateTime Timestamp { get; set; }
}