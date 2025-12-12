using System;
using Newtonsoft.Json;

namespace FortnitePorting.Models.API.Responses;

public class MappingsResponse
{
    public string Version;
    public DateTime? Updated;
    [JsonProperty("hash-md5")] public string HashMD5;
    public string Url;

    public DateTime GetCreationTime()
    {
        return Updated ?? DateTime.Now;
    }
}